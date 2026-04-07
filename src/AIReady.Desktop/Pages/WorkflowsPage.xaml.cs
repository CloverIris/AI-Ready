#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AIReady.Local.Core;
using AIReady.Local.Core.Hardware;
using AIReady.Local.Core.Miniconda;
using AIReady.Local.Core.Workflows;
using AIReady.Shared.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AIReady.Desktop.Pages
{
    public sealed partial class WorkflowsPage : Page
    {
        private readonly WorkflowRegistry _workflowRegistry;
        private readonly HardwareDetector _hardwareDetector;
        private readonly MinicondaManager _minicondaManager;
        private readonly WorkflowInstaller _workflowInstaller;
        private HardwareInfo? _hardwareInfo;

        public ObservableCollection<WorkflowTemplate> WorkflowTemplates { get; } = new();

        public WorkflowsPage()
        {
            this.InitializeComponent();
            
            _workflowRegistry = LocalCoreServices.CreateWorkflowRegistry();
            _hardwareDetector = LocalCoreServices.CreateHardwareDetector();
            _minicondaManager = LocalCoreServices.CreateMinicondaManager();
            _workflowInstaller = LocalCoreServices.CreateWorkflowInstaller(_minicondaManager);

            WorkflowsGridView.ItemsSource = WorkflowTemplates;
            
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Load hardware info
                _hardwareInfo = await _hardwareDetector.GetHardwareInfoAsync();

                // Load workflow templates
                await _workflowRegistry.LoadTemplatesAsync();
                var templates = _workflowRegistry.GetAllTemplates();

                WorkflowTemplates.Clear();
                
                // Filter compatible templates based on hardware
                var compatibleTemplates = templates.Where(t => IsCompatible(t, _hardwareInfo)).ToList();
                
                foreach (var template in compatibleTemplates)
                {
                    WorkflowTemplates.Add(template);
                }

                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;

                if (WorkflowTemplates.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
                
                await ShowErrorDialogAsync("加载失败", $"无法加载工作流列表: {ex.Message}");
            }
        }

        private bool IsCompatible(WorkflowTemplate template, HardwareInfo hardware)
        {
            if (template.Requirements == null) return true;

            var primaryGpu = hardware.GPUs.FirstOrDefault();
            var vramGb = primaryGpu?.VramBytes / (1024.0 * 1024 * 1024) ?? 0;
            var ramGb = hardware.TotalMemoryBytes / (1024.0 * 1024 * 1024);

            // Check VRAM
            if (vramGb < template.Requirements.MinVramGB)
            {
                // Still show but mark as not recommended
                return true; // Show all, user can decide
            }

            return true;
        }

        private async void WorkflowsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not WorkflowTemplate template) return;

            // Check compatibility
            var compatibility = GetCompatibilityStatus(template, _hardwareInfo);
            
            if (!compatibility.IsCompatible)
            {
                var confirmResult = await ShowConfirmDialogAsync(
                    "硬件要求不满足",
                    $"{compatibility.Message}\n\n仍要继续安装吗？");
                
                if (!confirmResult) return;
            }

            // Check Miniconda
            var isMinicondaInstalled = await _minicondaManager.IsInstalledAsync();
            if (!isMinicondaInstalled)
            {
                await ShowInfoDialogAsync(
                    "需要 Miniconda",
                    "安装工作流需要 Miniconda。请先前往「本地 AI」页面安装 Miniconda。");
                return;
            }

            // Show install dialog
            await ShowInstallDialogAsync(template);
        }

        private (bool IsCompatible, string Message) GetCompatibilityStatus(WorkflowTemplate template, HardwareInfo? hardware)
        {
            if (hardware == null || template.Requirements == null)
                return (true, "");

            var primaryGpu = hardware.GPUs.FirstOrDefault();
            var vramGb = primaryGpu?.VramBytes / (1024.0 * 1024 * 1024) ?? 0;

            if (vramGb < template.Requirements.MinVramGB)
            {
                return (false, $"您的显卡显存为 {vramGb:F0}GB，而此工作流需要至少 {template.Requirements.MinVramGB}GB 显存。运行可能会很慢或失败。");
            }

            if (vramGb < template.Requirements.RecommendedVramGB)
            {
                return (true, $"您的显卡显存为 {vramGb:F0}GB，低于推荐的 {template.Requirements.RecommendedVramGB}GB。可以运行但性能可能不佳。");
            }

            return (true, "");
        }

        private async Task ShowInstallDialogAsync(WorkflowTemplate template)
        {
            var progressRing = new ProgressRing { IsActive = true, Width = 32, Height = 32 };
            var progressText = new TextBlock 
            { 
                Text = "准备安装...",
                Margin = new Thickness(0, 12, 0, 0)
            };

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(progressRing);
            contentPanel.Children.Add(progressText);

            var dialog = new ContentDialog
            {
                Title = $"安装 {template.Name}",
                Content = contentPanel,
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var progress = new Progress<string>(msg =>
            {
                progressText.Text = msg;
            });

            var installTask = Task.Run(async () =>
            {
                try
                {
                    var instance = await _workflowInstaller.InstallAsync(template, null, progress);
                    
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        dialog.Hide();
                        await ShowInfoDialogAsync("安装成功", 
                            $"{template.Name} 安装完成！\n\n安装位置: {instance.InstallPath}");
                    });
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        dialog.Hide();
                        await ShowErrorDialogAsync("安装失败", ex.Message);
                    });
                }
            });

            await dialog.ShowAsync();
        }

        private async Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "继续",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task ShowInfoDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowErrorDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
