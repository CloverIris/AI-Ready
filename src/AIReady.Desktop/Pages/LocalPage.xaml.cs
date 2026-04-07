#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AIReady.Local.Core;
using AIReady.Local.Core.Miniconda;
using AIReady.Shared.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace AIReady.Desktop.Pages
{
    public sealed partial class LocalPage : Page
    {
        private readonly MinicondaManager _minicondaManager;
        private readonly ObservableCollection<EnvironmentInfo> _environments;

        public LocalPage()
        {
            this.InitializeComponent();
            _minicondaManager = LocalCoreServices.CreateMinicondaManager();
            _environments = new ObservableCollection<EnvironmentInfo>();
            EnvironmentsList.ItemsSource = _environments;
            
            _ = CheckMinicondaStatusAsync();
        }

        private async Task CheckMinicondaStatusAsync()
        {
            try
            {
                MinicondaStatusText.Text = "正在检测 Miniconda...";
                var isInstalled = await _minicondaManager.IsInstalledAsync();
                
                if (isInstalled)
                {
                    var path = await _minicondaManager.GetInstallPathAsync();
                    MinicondaStatusText.Text = $"Miniconda 已安装: {path}";
                    InstallMinicondaButton.Visibility = Visibility.Collapsed;
                    CreateEnvironmentButton.Visibility = Visibility.Visible;
                    
                    await LoadEnvironmentsAsync();
                }
                else
                {
                    MinicondaStatusText.Text = "Miniconda 未安装 - 需要安装以管理 Python 环境";
                    InstallMinicondaButton.Visibility = Visibility.Visible;
                    CreateEnvironmentButton.Visibility = Visibility.Collapsed;
                    ShowEmptyEnvironmentState();
                }
            }
            catch (Exception ex)
            {
                MinicondaStatusText.Text = $"检测失败: {ex.Message}";
                InstallMinicondaButton.Visibility = Visibility.Collapsed;
                CreateEnvironmentButton.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadEnvironmentsAsync()
        {
            try
            {
                _environments.Clear();
                var envs = await _minicondaManager.ListEnvironmentsAsync();
                
                foreach (var env in envs)
                {
                    _environments.Add(env);
                }

                if (_environments.Count > 0)
                {
                    EnvironmentsHeader.Visibility = Visibility.Visible;
                    EnvironmentsBorder.Visibility = Visibility.Visible;
                    EmptyEnvironmentState.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ShowEmptyEnvironmentState();
                }
            }
            catch (Exception ex)
            {
                MinicondaStatusText.Text = $"加载环境列表失败: {ex.Message}";
            }
        }

        private void ShowEmptyEnvironmentState()
        {
            EnvironmentsHeader.Visibility = Visibility.Visible;
            EnvironmentsBorder.Visibility = Visibility.Collapsed;
            EmptyEnvironmentState.Visibility = Visibility.Visible;
        }

        private async void InstallMinicondaButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await ShowConfirmDialogAsync(
                "安装 Miniconda",
                "这将下载并安装 Miniconda（约 100MB），安装过程需要管理员权限。\n\n是否继续？");
            
            if (!result) return;

            InstallMinicondaButton.IsEnabled = false;
            MinicondaInstallProgress.Visibility = Visibility.Visible;
            MinicondaInstallLog.Visibility = Visibility.Visible;

            var progress = new Progress<double>(value =>
            {
                MinicondaInstallProgress.Value = value;
                
                if (value < 0.4)
                    MinicondaInstallLog.Text = "正在下载安装程序...";
                else if (value < 0.9)
                    MinicondaInstallLog.Text = "正在安装...";
                else
                    MinicondaInstallLog.Text = "安装完成，正在初始化...";
            });

            try
            {
                var installResult = await _minicondaManager.InstallAsync(@"C:\ProgramData\miniconda3", progress);
                
                if (installResult.Success)
                {
                    await ShowInfoDialogAsync("安装成功", "Miniconda 安装完成！");
                    await CheckMinicondaStatusAsync();
                }
                else
                {
                    await ShowErrorDialogAsync("安装失败", installResult.Error ?? "未知错误");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("安装失败", ex.Message);
            }
            finally
            {
                InstallMinicondaButton.IsEnabled = true;
                MinicondaInstallProgress.Visibility = Visibility.Collapsed;
                MinicondaInstallLog.Visibility = Visibility.Collapsed;
            }
        }

        private async void CreateEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            var nameTextBox = new TextBox
            {
                Header = "环境名称",
                PlaceholderText = "例如: ollama-env"
            };
            
            var versionCombo = new ComboBox
            {
                Header = "Python 版本",
                SelectedIndex = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            versionCombo.Items.Add("3.9");
            versionCombo.Items.Add("3.10");
            versionCombo.Items.Add("3.11");
            versionCombo.Items.Add("3.12");
            
            var panel = new StackPanel { Spacing = 12, Width = 300 };
            panel.Children.Add(nameTextBox);
            panel.Children.Add(versionCombo);

            var dialog = new ContentDialog
            {
                Title = "创建 Conda 环境",
                Content = panel,
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var envName = nameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(envName))
            {
                await ShowErrorDialogAsync("错误", "请输入环境名称");
                return;
            }

            var pythonVersion = versionCombo.SelectedItem?.ToString() ?? "3.10";

            try
            {
                CreateEnvironmentButton.IsEnabled = false;
                MinicondaStatusText.Text = $"正在创建环境 '{envName}'...";
                
                await _minicondaManager.CreateEnvironmentAsync(envName, pythonVersion);
                
                await LoadEnvironmentsAsync();
                MinicondaStatusText.Text = $"环境 '{envName}' 创建成功";
            }
            catch (Exception ex)
            {
                await ShowErrorDialogAsync("创建失败", ex.Message);
            }
            finally
            {
                CreateEnvironmentButton.IsEnabled = true;
            }
        }

        private async void DeleteEnvironmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string envName)
            {
                var result = await ShowConfirmDialogAsync(
                    "删除环境",
                    $"确定要删除环境 '{envName}' 吗？此操作不可撤销。");
                
                if (!result) return;

                try
                {
                    MinicondaStatusText.Text = $"正在删除环境 '{envName}'...";
                    var success = await _minicondaManager.RemoveEnvironmentAsync(envName);
                    
                    if (success)
                    {
                        await LoadEnvironmentsAsync();
                        MinicondaStatusText.Text = $"环境 '{envName}' 已删除";
                    }
                    else
                    {
                        await ShowErrorDialogAsync("删除失败", "无法删除环境");
                    }
                }
                catch (Exception ex)
                {
                    await ShowErrorDialogAsync("删除失败", ex.Message);
                }
            }
        }

        private async Task<bool> ShowConfirmDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "确定",
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

    /// <summary>
    /// Converter for inverse boolean
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;
            return value;
        }
    }
}
