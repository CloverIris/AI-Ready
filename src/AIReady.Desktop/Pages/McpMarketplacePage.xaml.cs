#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.McpRegistry;
using AIReady.Shared.Services;
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
    /// MCP 市场页面 - 浏览和管理 Model Context Protocol 服务器
    /// 数据来源: https://registry.modelcontextprotocol.io/v0/servers
    /// </summary>
    public sealed partial class McpMarketplacePage : Page
    {
        /// <summary>
        /// UI 绑定的 MCP Server 视图模型
        /// </summary>
        public class McpServerViewModel
        {
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string? IconUrl { get; set; }
            public string StatusDisplay { get; set; } = string.Empty;
            public Color StatusColor { get; set; }
            public string TransportTypeDisplay { get; set; } = string.Empty;
            public List<string> TransportTypes { get; set; } = new();
            public string? WebsiteUrl { get; set; }
            public string? RepositoryUrl { get; set; }
            public McpServerEntry OriginalEntry { get; set; } = null!;
        }

        private readonly IMcpRegistryClient _mcpClient;
        private List<McpServerViewModel> _allServers = new();
        private List<McpServerViewModel> _filteredServers = new();
        private string _currentSearch = string.Empty;
        private string _currentTransportFilter = string.Empty;
        private CancellationTokenSource? _loadingCts;

        public McpMarketplacePage()
        {
            try
            {
                FileLogger.Log("McpMarketplacePage 构造函数开始", "DEBUG");
                this.InitializeComponent();
                FileLogger.Log("InitializeComponent 完成", "DEBUG");
                _mcpClient = new McpRegistryApiClient();
                FileLogger.Log("McpRegistryApiClient 创建完成", "DEBUG");
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "McpMarketplacePage 构造函数失败");
                throw;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            FileLogger.Log("OnNavigatedTo 被调用", "DEBUG");
            base.OnNavigatedTo(e);
            
            // 设置默认选中"全部"按钮
            SetDefaultCategoryButton();
            
            // 页面加载时自动获取数据
            _ = LoadServersAsync();
            FileLogger.Log("LoadServersAsync 已启动", "DEBUG");
        }

        /// <summary>
        /// 设置默认选中的分类按钮（"全部"）
        /// </summary>
        private void SetDefaultCategoryButton()
        {
            if (CategoryFilterPanel.Children.Count > 0 && CategoryFilterPanel.Children[0] is Button allButton)
            {
                UpdateCategoryButtonStates(allButton);
            }
        }

        /// <summary>
        /// 从 MCP Registry API 加载服务器列表
        /// </summary>
        private async Task LoadServersAsync()
        {
            FileLogger.Log("LoadServersAsync 开始", "DEBUG");
            
            // 取消之前的加载任务
            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            var cancellationToken = _loadingCts.Token;

            try
            {
                // 显示加载状态
                FileLogger.Log("设置加载状态", "DEBUG");
                LoadingProgressBar.Visibility = Visibility.Visible;
                ErrorInfoBar.IsOpen = false;
                McpServersList.ItemsSource = null;
                EmptyStateText.Visibility = Visibility.Collapsed;
                AppendLog("正在连接 MCP Registry...");

                AppendLog("正在获取 MCP Servers 列表...");
                FileLogger.Log("调用 GetServersAsync", "DEBUG");

                // 直接获取数据（同时检查可用性）
                var response = await _mcpClient.GetServersAsync(
                    limit: 50,
                    search: string.IsNullOrEmpty(_currentSearch) ? null : _currentSearch,
                    cancellationToken: cancellationToken);
                
                FileLogger.Log($"GetServersAsync 返回，服务器数量: {response.Servers.Count}", "DEBUG");

                // 转换为视图模型
                FileLogger.Log("转换为视图模型", "DEBUG");
                _allServers = response.Servers
                    .Where(s => s.Meta.Official?.Status == McpServerStatus.Active) // 只显示活跃的
                    .Select(ConvertToViewModel)
                    .ToList();

                _filteredServers = _allServers;

                AppendLog($"[INFO] 成功加载 {_allServers.Count} 个 MCP Servers");

                // 更新 UI
                FileLogger.Log("更新 UI", "DEBUG");
                UpdateStats();
                ApplyFilters();
                FileLogger.Log("LoadServersAsync 成功完成", "DEBUG");
            }
            catch (OperationCanceledException)
            {
                AppendLog("[INFO] 加载已取消");
                FileLogger.Log("加载已取消", "DEBUG");
            }
            catch (McpRegistryException ex)
            {
                FileLogger.LogException(ex, "McpRegistryException");
                var innerMsg = ex.InnerException?.Message ?? "未知网络错误";
                AppendLog($"[WARNING] MCP API 错误: {innerMsg}");
                ShowError($"无法连接到 MCP Registry，显示示例数据。\n错误: {innerMsg}");
                LoadMockData();
            }
            catch (HttpRequestException ex)
            {
                FileLogger.LogException(ex, "HttpRequestException");
                AppendLog($"[WARNING] 网络请求失败: {ex.Message}");
                ShowError($"无法连接到 MCP Registry，显示示例数据。\n错误: {ex.Message}");
                LoadMockData();
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "LoadServersAsync 未处理异常");
                var errorDetails = $"[ERROR] 加载失败: {ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetails += $"\n  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                }
                AppendLog(errorDetails);
                ShowError($"API 连接失败，显示示例数据。错误: {ex.Message}");
                
                // 加载示例数据作为备用
                LoadMockData();
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                FileLogger.Log("LoadServersAsync finally", "DEBUG");
            }
        }

        /// <summary>
        /// 转换为视图模型
        /// </summary>
        private McpServerViewModel ConvertToViewModel(McpServerEntry entry)
        {
            var server = entry.Server;
            var meta = entry.Meta.Official;

            var transportTypes = server.GetTransportTypes();
            var transportDisplay = transportTypes.Count > 0
                ? string.Join(", ", transportTypes)
                : "未知";

            var (statusText, statusColor) = meta?.Status switch
            {
                McpServerStatus.Active => ("活跃", Colors.Green),
                McpServerStatus.Deprecated => ("已弃用", Colors.Orange),
                McpServerStatus.Suspended => ("已暂停", Colors.Red),
                _ => ("未知", Colors.Gray)
            };

            return new McpServerViewModel
            {
                Name = server.Name,
                DisplayName = server.DisplayName,
                Description = server.Description,
                Version = server.Version,
                IconUrl = server.PrimaryIconUrl,
                StatusDisplay = statusText,
                StatusColor = statusColor,
                TransportTypeDisplay = transportDisplay,
                TransportTypes = transportTypes,
                WebsiteUrl = server.WebsiteUrl,
                RepositoryUrl = server.Repository?.Url,
                OriginalEntry = entry
            };
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStats()
        {
            TotalServersText.Text = _allServers.Count.ToString();
            ActiveServersText.Text = _allServers.Count.ToString(); // 目前 API 只返回活跃的
            
            var uniqueTransports = _allServers
                .SelectMany(s => s.TransportTypes)
                .Distinct()
                .Count();
            TransportTypesText.Text = uniqueTransports.ToString();
            
            LastUpdatedText.Text = DateTime.Now.ToString("HH:mm");
        }

        /// <summary>
        /// 应用筛选器
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = _allServers.AsEnumerable();

            // 应用传输类型筛选
            if (!string.IsNullOrEmpty(_currentTransportFilter))
            {
                filtered = filtered.Where(s => 
                    s.TransportTypes.Any(t => t.Equals(_currentTransportFilter, StringComparison.OrdinalIgnoreCase)));
            }

            _filteredServers = filtered.ToList();
            McpServersList.ItemsSource = _filteredServers;
            
            // 显示/隐藏空状态
            EmptyStateText.Visibility = _filteredServers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 搜索框提交
        /// </summary>
        private void McpSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            _currentSearch = args.QueryText ?? string.Empty;
            _ = LoadServersAsync();
        }

        /// <summary>
        /// 刷新按钮点击
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadServersAsync();
        }

        /// <summary>
        /// 传输类型筛选
        /// </summary>
        private void FilterTransport_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                _currentTransportFilter = item.Tag?.ToString() ?? string.Empty;
                AppendLog($"[INFO] 筛选传输类型: {_currentTransportFilter ?? "全部"}");
                ApplyFilters();
            }
        }

        /// <summary>
        /// 分类筛选（暂用关键词匹配）
        /// </summary>
        private void FilterCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var category = button.Tag?.ToString() ?? string.Empty;
                _currentSearch = category;
                McpSearchBox.Text = category;
                
                // 更新按钮选中状态
                UpdateCategoryButtonStates(button);
                
                _ = LoadServersAsync();
            }
        }

        /// <summary>
        /// 更新分类按钮的选中状态
        /// </summary>
        private void UpdateCategoryButtonStates(Button selectedButton)
        {
            // 遍历所有分类按钮，更新样式
            foreach (var child in CategoryFilterPanel.Children)
            {
                if (child is Button btn)
                {
                    if (btn == selectedButton)
                    {
                        // 选中按钮使用强调色样式
                        btn.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
                    }
                    else
                    {
                        // 未选中按钮使用默认样式
                        btn.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
                    }
                }
            }
        }

        /// <summary>
        /// MCP Server 项点击事件 - 打开详情
        /// </summary>
        private void McpServerItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is McpServerViewModel server)
            {
                AppendLog($"[INFO] 查看 MCP Server: {server.Name}");
                ShowNotImplementedDialog($"查看 {server.DisplayName} 详情\n\n描述: {server.Description}\n版本: {server.Version}\n传输: {server.TransportTypeDisplay}");
            }
        }

        /// <summary>
        /// 查看按钮点击
        /// </summary>
        private void ViewMcpServer_Click(object sender, RoutedEventArgs e)
        {
            // 获取点击项的数据上下文
            if (sender is Button button && button.DataContext is McpServerViewModel server)
            {
                AppendLog($"[INFO] 查看 MCP Server: {server.Name}");
                ShowNotImplementedDialog($"查看 {server.DisplayName} 详情\n\n描述: {server.Description}\n版本: {server.Version}\n传输: {server.TransportTypeDisplay}");
            }
        }

        /// <summary>
        /// 添加自定义 MCP Server
        /// </summary>
        private void AddMcpServer_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("[INFO] 打开添加 MCP Server 对话框");
            ShowNotImplementedDialog("添加自定义 MCP Server\n\n功能开发中：\n- 支持手动输入 server.json\n- 支持从 URL 导入\n- 支持本地路径选择");
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowError(string message)
        {
            ErrorInfoBar.Message = message;
            ErrorInfoBar.IsOpen = true;
        }

        /// <summary>
        /// 添加日志（同时写入文件和控制台）
        /// </summary>
        private void AppendLog(string message)
        {
            // 写入文件日志
            FileLogger.Log(message, "MCP");
            
            // 更新 UI
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            McpLogsText.Text += $"\n[{timestamp}] {message}";
        }

        /// <summary>
        /// 加载示例数据（API 不可用时使用）
        /// </summary>
        private void LoadMockData()
        {
            try
            {
                AppendLog("[INFO] 加载示例数据...");
                
                _allServers = new List<McpServerViewModel>
            {
                new()
                {
                    Name = "io.github.filesystem/server",
                    DisplayName = "文件系统访问",
                    Description = "允许 AI 安全地读取和写入本地文件系统中的文件",
                    Version = "1.0.0",
                    TransportTypes = new List<string> { "stdio" },
                    TransportTypeDisplay = "stdio",
                    StatusDisplay = "活跃",
                    StatusColor = Colors.Green
                },
                new()
                {
                    Name = "io.github.sqlite/server",
                    DisplayName = "SQLite 数据库",
                    Description = "通过 MCP 协议查询 SQLite 数据库",
                    Version = "1.0.0",
                    TransportTypes = new List<string> { "stdio" },
                    TransportTypeDisplay = "stdio",
                    StatusDisplay = "活跃",
                    StatusColor = Colors.Green
                },
                new()
                {
                    Name = "io.github.github/server",
                    DisplayName = "GitHub 集成",
                    Description = "管理 Issues、PR、代码仓库的 MCP 服务器",
                    Version = "1.0.0",
                    TransportTypes = new List<string> { "stdio" },
                    TransportTypeDisplay = "stdio",
                    StatusDisplay = "活跃",
                    StatusColor = Colors.Green
                },
                new()
                {
                    Name = "io.github.fetch/server",
                    DisplayName = "Web 抓取",
                    Description = "获取和解析网页内容的 MCP 服务器",
                    Version = "1.0.0",
                    TransportTypes = new List<string> { "stdio" },
                    TransportTypeDisplay = "stdio",
                    StatusDisplay = "活跃",
                    StatusColor = Colors.Green
                },
                new()
                {
                    Name = "io.github.brave/server",
                    DisplayName = "Brave 搜索",
                    Description = "使用 Brave Search API 进行网络搜索",
                    Version = "1.0.0",
                    TransportTypes = new List<string> { "streamable-http" },
                    TransportTypeDisplay = "streamable-http",
                    StatusDisplay = "活跃",
                    StatusColor = Colors.Green
                }
            };

                _filteredServers = _allServers;
                UpdateStats();
                ApplyFilters();
                
                AppendLog($"[INFO] 已加载 {_allServers.Count} 个示例 MCP Servers");
            }
            catch (Exception ex)
            {
                // 如果连示例数据都加载失败，显示空状态
                AppendLog($"[ERROR] 加载示例数据失败: {ex.Message}");
                _allServers = new List<McpServerViewModel>();
                _filteredServers = _allServers;
                EmptyStateText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 显示功能开发中提示
        /// </summary>
        private async void ShowNotImplementedDialog(string feature)
        {
            var dialog = new ContentDialog
            {
                Title = "MCP Server 详情",
                Content = feature,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
