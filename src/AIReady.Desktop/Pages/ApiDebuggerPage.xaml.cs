#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AIReady.Shared.Models;
using AIReady.Shared.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace AIReady.Desktop.Pages
{
    /// <summary>
    /// API 调试器页面 - 本地 API 代理、调试、审计、过期提醒
    /// </summary>
    public sealed partial class ApiDebuggerPage : Page
    {
        // 防止多个 ContentDialog 同时显示
        private static bool _isDialogShowing = false;
        
        // 视图模型
        public class EndpointViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public ApiEndpointType Type { get; set; }
            public string TypeDisplay => Type.ToString();
            public string TypeIcon => Type switch
            {
                ApiEndpointType.OpenAI => "\uE756",
                ApiEndpointType.Anthropic => "\uE8C4",
                ApiEndpointType.AzureOpenAI => "\uE774",
                ApiEndpointType.GoogleGemini => "\uE774",
                _ => "\uE774"
            };
            public string BalanceDisplay { get; set; } = "--";
            public string LatencyDisplay { get; set; } = "--";
            public string ExpirationDisplay { get; set; } = "--";
            public Color ExpirationColor { get; set; } = Colors.White;
            public string StatusIcon { get; set; } = "\uE73E";
            public Brush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);
            public bool IsShared { get; set; } = true;
            public ApiEndpoint Original { get; set; } = null!;
        }

        public class DeviceViewModel
        {
            public string DeviceName { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public string DeviceIcon { get; set; } = "\uE770";
            public string CurrentApi { get; set; } = "空闲";
        }

        public class ModelUsageViewModel
        {
            public string ModelName { get; set; } = string.Empty;
            public string Percentage { get; set; } = "0%";
            public double PercentageValue { get; set; }
        }

        private ObservableCollection<EndpointViewModel> _endpoints = new();
        private ObservableCollection<DeviceViewModel> _devices = new();
        private ObservableCollection<ModelUsageViewModel> _modelUsages = new();

        public ApiDebuggerPage()
        {
            this.InitializeComponent();
            LoadMockData();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            RefreshDisplay();
        }

        /// <summary>
        /// 加载示例数据
        /// </summary>
        private void LoadMockData()
        {
            _endpoints = new ObservableCollection<EndpointViewModel>
            {
                new()
                {
                    Id = "1",
                    Name = "OpenAI Production",
                    Type = ApiEndpointType.OpenAI,
                    BalanceDisplay = "$12.50",
                    LatencyDisplay = "245ms",
                    ExpirationDisplay = "23天",
                    ExpirationColor = Colors.LightGreen,
                    StatusIcon = "\uE73E",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    IsShared = true
                },
                new()
                {
                    Id = "2",
                    Name = "Claude Enterprise",
                    Type = ApiEndpointType.Anthropic,
                    BalanceDisplay = "$45.00",
                    LatencyDisplay = "180ms",
                    ExpirationDisplay = "67天",
                    ExpirationColor = Colors.LightGreen,
                    StatusIcon = "\uE73E",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    IsShared = true
                },
                new()
                {
                    Id = "3",
                    Name = "Azure East Asia",
                    Type = ApiEndpointType.AzureOpenAI,
                    BalanceDisplay = "20%",
                    LatencyDisplay = "320ms",
                    ExpirationDisplay = "15天 ⚠️",
                    ExpirationColor = Colors.Orange,
                    StatusIcon = "\uE7BA",
                    StatusColor = new SolidColorBrush(Colors.Orange),
                    IsShared = false
                },
                new()
                {
                    Id = "4",
                    Name = "Gemini Pro",
                    Type = ApiEndpointType.GoogleGemini,
                    BalanceDisplay = "无限",
                    LatencyDisplay = "150ms",
                    ExpirationDisplay = "无限期",
                    ExpirationColor = Colors.White,
                    StatusIcon = "\uE73E",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    IsShared = true
                }
            };

            _devices = new ObservableCollection<DeviceViewModel>
            {
                new() { DeviceName = "iPhone 15", IpAddress = "192.168.1.101", CurrentApi = "使用中: OpenAI", DeviceIcon = "\uE80F" },
                new() { DeviceName = "MacBook Pro", IpAddress = "192.168.1.102", CurrentApi = "使用中: Claude", DeviceIcon = "\uE7F4" },
                new() { DeviceName = "iPad Air", IpAddress = "192.168.1.103", CurrentApi = "空闲", DeviceIcon = "\uE7EA" }
            };

            _modelUsages = new ObservableCollection<ModelUsageViewModel>
            {
                new() { ModelName = "GPT-4", Percentage = "45%", PercentageValue = 45 },
                new() { ModelName = "Claude-3.5", Percentage = "30%", PercentageValue = 30 },
                new() { ModelName = "GPT-3.5", Percentage = "15%", PercentageValue = 15 },
                new() { ModelName = "其他", Percentage = "10%", PercentageValue = 10 }
            };
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        private void RefreshDisplay()
        {
            EndpointGrid.ItemsSource = _endpoints;
            ConnectedDevicesList.ItemsSource = _devices;
            ModelUsageList.ItemsSource = _modelUsages;

            TodayCallsText.Text = "1,234";
            ConnectedDevicesText.Text = _devices.Count.ToString();
            EstimatedCostText.Text = "$3.45";

            // 检查过期提醒
            var expiringCount = _endpoints.Count(e => e.ExpirationColor == Colors.Orange || e.ExpirationColor == Colors.Red);
            if (expiringCount > 0)
            {
                ExpirationAlertBar.Message = $"有 {expiringCount} 个 API 将在 7 天内过期或余额不足 20%";
                ExpirationAlertBar.IsOpen = true;
            }
        }

        #region 事件处理

        private void AddEndpointButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("添加 API 端点");
        }

        private void RefreshAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("全部检测");
        }

        private void ProxySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("代理设置");
        }

        private void ProxyServiceToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var isOn = ProxyServiceToggle.IsOn;
            ProxyStatusText.Text = isOn ? "代理服务运行中" : "代理服务已停止";
            ProxyStatusIcon.Foreground = isOn 
                ? Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush ?? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Gray);
        }

        private void ViewExpiringApis_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("查看即将过期的 API");
        }

        private void EndpointItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EndpointViewModel endpoint)
            {
                ShowNotImplementedDialog($"查看端点详情: {endpoint.Name}");
            }
        }

        private void TestEndpoint_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("测试端点连接");
        }

        private void EditEndpoint_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("编辑端点配置");
        }

        private void ToggleShare_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                var isOn = toggle.IsOn;
                ShowNotImplementedDialog($"切换共享状态: {(isOn ? "开启" : "关闭")}");
            }
        }

        private void DisconnectDevice_Click(object sender, RoutedEventArgs e)
        {
            ShowNotImplementedDialog("断开设备连接");
        }

        #endregion

        /// <summary>
        /// 显示功能开发中提示（防止重复显示）
        /// </summary>
        private async void ShowNotImplementedDialog(string feature)
        {
            // 防止多个 ContentDialog 同时显示
            if (_isDialogShowing)
                return;

            try
            {
                _isDialogShowing = true;
                
                var dialog = new ContentDialog
                {
                    Title = "功能开发中",
                    Content = $"'{feature}' 功能正在开发中，敬请期待！\n\n完整功能包括：\n- API 端点管理\n- 健康检测\n- 局域网代理\n- 审计统计",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // 忽略对话框显示异常
                System.Diagnostics.Debug.WriteLine($"Dialog show failed: {ex.Message}");
            }
            finally
            {
                _isDialogShowing = false;
            }
        }
    }
}
