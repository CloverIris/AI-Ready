using Microsoft.UI.Xaml;

namespace AIRaedy.Desktop
{
    public partial class App : Application
    {
        private Window? m_window;
        public Window? Window => m_window;

        public App()
        {
            this.InitializeComponent();
            this.RequestedTheme = ApplicationTheme.Dark;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            
            // 确保窗口使用深色主题
            if (m_window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Dark;
            }
            
            m_window.Activate();
        }
    }
}
