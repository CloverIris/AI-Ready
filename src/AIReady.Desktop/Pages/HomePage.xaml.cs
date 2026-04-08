#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using AIReady.Local.Core;
using AIReady.Local.Core.Hardware;
using AIReady.Shared.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace AIReady.Desktop.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly NavigationTransitionInfo _transitionInfo = new DrillInNavigationTransitionInfo();
        private readonly HardwareDetector _hardwareDetector;
        private readonly CompatibilityChecker _compatibilityChecker;
        private HardwareInfo? _hardwareInfo;

        public HomePage()
        {
            this.InitializeComponent();
            _hardwareDetector = LocalCoreServices.CreateHardwareDetector();
            _compatibilityChecker = LocalCoreServices.CreateCompatibilityChecker();
            
            _ = LoadHardwareInfoAsync();
        }

        private async Task LoadHardwareInfoAsync()
        {
            try
            {
                HardwareInfoText.Text = "正在检测硬件信息...";
                
                _hardwareInfo = await _hardwareDetector.GetHardwareInfoAsync();
                var compatibility = _compatibilityChecker.AssessCompatibility(_hardwareInfo);
                
                UpdateHardwareDisplay(_hardwareInfo, compatibility);
            }
            catch (Exception ex)
            {
                HardwareInfoText.Text = $"硬件检测失败: {ex.Message}";
            }
        }

        private void UpdateHardwareDisplay(HardwareInfo info, CompatibilityResult compatibility)
        {
            var lines = new System.Collections.Generic.List<string>();
            
            // CPU info
            if (!string.IsNullOrEmpty(info.CpuModel))
            {
                lines.Add($"CPU: {info.CpuModel} ({info.CpuCores}核 {info.CpuThreads}线程)");
            }
            
            // Memory info
            var ramGb = info.TotalMemoryBytes / (1024.0 * 1024 * 1024);
            lines.Add($"内存: {ramGb:F1} GB");
            
            // GPU info
            if (info.GPUs.Count > 0)
            {
                foreach (var gpu in info.GPUs)
                {
                    var vramGb = gpu.VramBytes / (1024.0 * 1024 * 1024);
                    if (vramGb > 0)
                    {
                        lines.Add($"GPU: {gpu.Name} ({vramGb:F0} GB)");
                    }
                    else
                    {
                        lines.Add($"GPU: {gpu.Name}");
                    }
                }
            }
            else
            {
                lines.Add("GPU: 未检测到独立显卡");
            }
            
            // Compatibility status
            var levelText = CompatibilityChecker.GetCompatibilityLevelDescription(compatibility.Level);
            var statusSymbol = compatibility.Level switch
            {
                CompatibilityLevel.Optimal => "✓",
                CompatibilityLevel.Recommended => "✓",
                CompatibilityLevel.Minimum => "!",
                _ => "✗"
            };
            
            lines.Add($"状态: {statusSymbol} {levelText}");
            
            // Show first warning/error if any
            var criticalIssue = compatibility.Issues.FirstOrDefault(i => i.Severity == IssueSeverity.Error || i.Severity == IssueSeverity.Warning);
            if (criticalIssue != null)
            {
                lines.Add($"提示: {criticalIssue.Description}");
            }
            
            HardwareInfoText.Text = string.Join(" | ", lines);
        }

        private void OpenLocalPage_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null)
            {
                Frame.Navigate(typeof(LocalPage), null, _transitionInfo);
            }
        }

        private void OpenCloudPage_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null)
            {
                Frame.Navigate(typeof(CloudPage), null, _transitionInfo);
            }
        }

    }
}
