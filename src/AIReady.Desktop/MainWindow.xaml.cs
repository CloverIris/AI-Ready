#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Animation;

namespace AIReady.Desktop
{
    public sealed partial class MainWindow : Window
    {
        // 用于窗口过程子类化，禁用标题栏双击放大或全屏
        private IntPtr oldWndProc = IntPtr.Zero;
        private WndProcDelegate? newWndProcDelegate;
        private const int GWL_WNDPROC = -4;
        private const uint WM_NCLBUTTONDBLCLK = 0x00A3;
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // 存储预加载的页面实例
        private readonly Dictionary<string, Page> _pages = new();
        private readonly NavigationTransitionInfo _transitionInfo;

        public MainWindow()
        {
            this.InitializeComponent();

            // 初始化页面切换动画
            _transitionInfo = new DrillInNavigationTransitionInfo();

            // 预加载页面
            _pages = new Dictionary<string, Page>
            {
                { "HomePage", new Pages.HomePage() },
                { "LocalPage", new Pages.LocalPage() },
                { "CloudPage", new Pages.CloudPage() },
                { "WorkflowsPage", new Pages.WorkflowsPage() },
                { "SkillsStorePage", new Pages.SkillsStorePage() },
                { "McpMarketplacePage", new Pages.McpMarketplacePage() },
                { "HelpPage", new Pages.HelpPage() },
                { "SettingsPage", new Pages.SettingsPage() }
            };

            // 设置默认页面
            ContentFrame.Navigate(typeof(Pages.HomePage), null, _transitionInfo);

            // 设置窗口大小并居中
            SetWindowSizeAndCenter(1000, 600);

            // 设置自定义标题栏
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // 禁用最大化功能
            DisableMaximize();

            // 注册窗口激活事件
            this.Activated += MainWindow_Activated;

            // 初始设置亚克力效果
            UpdateTitleBarBrush();

            // 子类化窗口过程，禁用标题栏双击
            SubclassWindowProc();
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.Deactivated)
            {
                UpdateTitleBarBrush();
            }
        }

        private void UpdateTitleBarBrush()
        {
            // 依据当前主题设置自定义标题栏的 Fill
            if (Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                AppTitleBar.Fill = Application.Current.Resources["CustomAcrylicInAppLuminosity"] as Brush;
            }
            else
            {
                AppTitleBar.Fill = Application.Current.Resources["AcrylicInAppFillColorDefaultBrush"] as Brush;
            }

            // 更新系统标题栏按钮颜色为白色
            UpdateTitleBarButtonColors(Colors.White);
        }

        private void SetWindowSizeAndCenter(int baseWidth, int baseHeight)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // 获取显示器 DPI
            var dpi = GetDpiForWindow(windowHandle);
            var scalingFactor = (float)dpi / 96;

            // 根据 DPI 缩放调整窗口大小
            var adjustedWidth = (int)(baseWidth * scalingFactor);
            var adjustedHeight = (int)(baseHeight * scalingFactor);

            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = adjustedWidth, Height = adjustedHeight });

            // 获取屏幕尺寸并居中窗口
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            if (displayArea != null)
            {
                var centerX = ((displayArea.WorkArea.Width - adjustedWidth) / 2);
                var centerY = ((displayArea.WorkArea.Height - adjustedHeight) / 2);
                appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
            }
        }

        private void DisableMaximize()
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsMaximizable = false;  // 禁用最大化按钮
                presenter.IsResizable = false;    // 禁止调整窗口大小
            }
        }

        private void SubclassWindowProc()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            newWndProcDelegate = new WndProcDelegate(CustomWndProc);
            oldWndProc = GetWindowLongPtr(hwnd, GWL_WNDPROC);
            SetWindowLongPtr(hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(newWndProcDelegate));
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_NCLBUTTONDBLCLK)
            {
                // 拦截双击非客户端区域（标题栏），返回 0 禁止窗口放大或全屏
                return IntPtr.Zero;
            }
            return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                string? tag = item.Tag?.ToString();
                if (tag != null && _pages.TryGetValue(tag, out var page))
                {
                    ContentFrame.Navigate(page.GetType(), null, _transitionInfo);
                }
            }
        }

        private void AppTitleBar_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;  // 阻止双击事件的传播
        }

        private void UpdateTitleBarButtonColors(Color buttonColor)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var titleBar = appWindow.TitleBar;
            if (titleBar != null)
            {
                titleBar.ButtonForegroundColor = buttonColor;
                titleBar.ButtonInactiveForegroundColor = buttonColor;
                titleBar.ButtonHoverForegroundColor = buttonColor;
                titleBar.ButtonPressedForegroundColor = buttonColor;

                titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(0xFF, 0x80, 0x80, 0x80);
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(0x22, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(0x44, 0xFF, 0xFF, 0xFF);
            }
        }

        // P/Invoke 声明
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);
    }
}
