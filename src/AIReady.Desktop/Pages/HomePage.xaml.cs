using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace AIReady.Desktop.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly NavigationTransitionInfo _transitionInfo = new DrillInNavigationTransitionInfo();

        public HomePage()
        {
            this.InitializeComponent();
            UpdateHardwareInfo();
        }

        private void UpdateHardwareInfo()
        {
            // 简化版硬件信息显示
            HardwareInfoText.Text = "系统就绪 | 等待检测硬件信息...";
        }

        private void OpenLocalPage_Click(object sender, RoutedEventArgs e)
        {
            // 导航到本地 AI 页面
            if (this.Frame != null)
            {
                Frame.Navigate(typeof(LocalPage), null, _transitionInfo);
            }
        }

        private void OpenCloudPage_Click(object sender, RoutedEventArgs e)
        {
            // 导航到云端管理页面
            if (this.Frame != null)
            {
                Frame.Navigate(typeof(CloudPage), null, _transitionInfo);
            }
        }

        private void QuickStartButton_Click(object sender, RoutedEventArgs e)
        {
            // 快速开始向导
            StatusText.Text = "正在启动向导...";
        }
    }
}
