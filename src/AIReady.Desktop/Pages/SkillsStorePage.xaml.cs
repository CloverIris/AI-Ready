#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.Skills;
using AIReady.Shared.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace AIReady.Desktop.Pages
{
    /// <summary>
    /// Skills 商店页面 - 浏览、搜索、安装和管理 AI Skills
    /// 数据来源: 本地静态数据 (内置 + 缓存)，未来将对接 GitHub anthropics/skills
    /// </summary>
    public sealed partial class SkillsStorePage : Page
    {
        /// <summary>
        /// UI 绑定的 Skill 视图模型
        /// </summary>
        public class SkillViewModel : INotifyPropertyChanged
        {
            private bool _isFavorited;

            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public double Rating { get; set; }
            public int InstallCount { get; set; }
            public DateTime LastUpdated { get; set; }

            public bool IsFavorited
            {
                get => _isFavorited;
                set
                {
                    _isFavorited = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorited)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavoriteIcon)));
                }
            }

            public string FavoriteIcon => IsFavorited ? "\uE735" : "\uE734"; // Filled vs Outline Heart

            public event PropertyChangedEventHandler? PropertyChanged;

            /// <summary>
            /// 从模型创建视图模型
            /// </summary>
            public static SkillViewModel FromSkillItem(SkillItem item)
            {
                return new SkillViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Author = item.Author,
                    Version = item.Version,
                    Category = item.Category,
                    Rating = item.Rating,
                    InstallCount = item.InstallCount,
                    LastUpdated = item.LastUpdated,
                    IsFavorited = item.IsFavorited
                };
            }
        }

        private readonly ISkillsRegistryClient _skillsClient;
        private List<SkillViewModel> _allSkills = new();
        private List<SkillViewModel> _filteredSkills = new();
        private string _currentSearch = string.Empty;
        private string _currentCategory = string.Empty;
        private string _currentSort = "popular";
        private CancellationTokenSource? _loadingCts;

        public SkillsStorePage()
        {
            this.InitializeComponent();
            // 使用静态数据源（内置示例数据）
            _skillsClient = new StaticSkillsClient();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // 页面加载时自动获取数据
            _ = LoadSkillsAsync();
        }

        /// <summary>
        /// 加载 Skills 数据
        /// </summary>
        private async Task LoadSkillsAsync()
        {
            // 取消之前的加载任务
            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            var cancellationToken = _loadingCts.Token;

            try
            {
                // 显示加载状态
                LoadingProgressBar.Visibility = Visibility.Visible;
                ErrorInfoBar.IsOpen = false;
                FeaturedSkillsGrid.ItemsSource = null;

                // 获取数据
                var skills = await _skillsClient.GetSkillsAsync(cancellationToken);

                // 转换为视图模型
                _allSkills = skills.Select(SkillViewModel.FromSkillItem).ToList();

                ApplyFilters();
                UpdateStats();
            }
            catch (OperationCanceledException)
            {
                // 加载已取消，忽略
            }
            catch (Exception ex)
            {
                ShowError($"加载失败: {ex.Message}");
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStats()
        {
            TotalSkillsText.Text = $"{_allSkills.Count} Skills 可用";
            LastUpdatedText.Text = $"最后更新: {DateTime.Now:HH:mm}";
        }

        /// <summary>
        /// 应用筛选和排序
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = _allSkills.AsEnumerable();

            // 应用搜索筛选
            if (!string.IsNullOrWhiteSpace(_currentSearch))
            {
                var search = _currentSearch.ToLowerInvariant();
                filtered = filtered.Where(s => 
                    s.Name.ToLowerInvariant().Contains(search) ||
                    s.Description.ToLowerInvariant().Contains(search));
            }

            // 应用分类筛选
            if (!string.IsNullOrEmpty(_currentCategory))
            {
                filtered = filtered.Where(s => s.Category == _currentCategory);
            }

            // 应用排序
            filtered = _currentSort switch
            {
                "newest" => filtered.OrderByDescending(s => s.LastUpdated),
                "rating" => filtered.OrderByDescending(s => s.Rating),
                "popular" => filtered.OrderByDescending(s => s.InstallCount),
                _ => filtered.OrderByDescending(s => s.InstallCount)
            };

            _filteredSkills = filtered.ToList();
            FeaturedSkillsGrid.ItemsSource = _filteredSkills;
        }

        /// <summary>
        /// 搜索框提交
        /// </summary>
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            _currentSearch = args.QueryText ?? string.Empty;
            ApplyFilters();
        }

        /// <summary>
        /// 分类筛选
        /// </summary>
        private void FilterCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                _currentCategory = item.Tag?.ToString() ?? string.Empty;
                ApplyFilters();
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                _currentSort = item.Tag?.ToString() ?? "popular";
                ApplyFilters();
            }
        }

        /// <summary>
        /// 刷新按钮点击
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadSkillsAsync();
        }

        /// <summary>
        /// Skill 卡片点击事件
        /// </summary>
        private void SkillItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is SkillViewModel skill)
            {
                ShowSkillDetail(skill);
            }
        }

        /// <summary>
        /// 查看按钮点击
        /// </summary>
        private void ViewSkill_Click(object sender, RoutedEventArgs e)
        {
            // 获取点击项的数据上下文
            if (sender is Button button)
            {
                var skill = FindSkillFromButton(button);
                if (skill != null)
                {
                    ShowSkillDetail(skill);
                }
            }
        }

        /// <summary>
        /// 收藏按钮点击
        /// </summary>
        private void FavoriteSkill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var skill = FindSkillFromButton(button);
                if (skill != null)
                {
                    skill.IsFavorited = !skill.IsFavorited;
                    // TODO: 保存收藏状态到本地存储
                }
            }
        }

        /// <summary>
        /// 从按钮查找对应的 Skill
        /// </summary>
        private SkillViewModel? FindSkillFromButton(Button button)
        {
            // 向上查找 Grid 容器
            var parent = button.Parent;
            while (parent != null && parent is not Grid)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is Grid grid)
            {
                // 查找 ListViewItem
                var listViewItem = FindParent<ListViewItem>(grid);
                if (listViewItem != null)
                {
                    return listViewItem.Content as SkillViewModel;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找父元素
        /// </summary>
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        /// <summary>
        /// 显示 Skill 详情
        /// </summary>
        private async void ShowSkillDetail(SkillViewModel skill)
        {
            var content = $"""
                名称: {skill.Name}
                作者: {skill.Author}
                版本: {skill.Version}
                分类: {skill.Category}
                评分: ⭐ {skill.Rating:F1}
                安装数: 📥 {skill.InstallCount}
                更新时间: {skill.LastUpdated:yyyy-MM-dd}

                描述:
                {skill.Description}

                功能开发中:
                - 安装 Skill
                - 查看源代码
                - 配置参数
                """;

            var dialog = new ContentDialog
            {
                Title = skill.Name,
                Content = content,
                PrimaryButtonText = "安装",
                CloseButtonText = "关闭",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // TODO: 实现安装逻辑
                ShowNotImplementedDialog($"安装 Skill: {skill.Name}");
            }
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
        /// 显示功能开发中提示
        /// </summary>
        private async void ShowNotImplementedDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "功能开发中",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
