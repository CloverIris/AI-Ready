#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.Skills;

namespace AIReady.Shared.Services
{
    /// <summary>
    /// 静态 Skills 数据源客户端
    /// 使用内置 JSON 数据作为离线备用
    /// </summary>
    public class StaticSkillsClient : ISkillsRegistryClient
    {
        private readonly string _dataPath;
        private SkillsRegistry _registry = new();

        public StaticSkillsClient(string? dataPath = null)
        {
            _dataPath = dataPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIReady",
                "skills-registry.json");
        }

        /// <inheritdoc />
        public Task<List<SkillItem>> GetSkillsAsync(CancellationToken cancellationToken = default)
        {
            EnsureLoaded();
            return Task.FromResult(_registry.Skills.ToList());
        }

        /// <inheritdoc />
        public Task<List<SkillItem>> SearchSkillsAsync(string query, CancellationToken cancellationToken = default)
        {
            EnsureLoaded();
            
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(_registry.Skills.ToList());

            var lowerQuery = query.ToLowerInvariant();
            var results = _registry.Skills
                .Where(s => 
                    s.Name.ToLowerInvariant().Contains(lowerQuery) ||
                    s.Description.ToLowerInvariant().Contains(lowerQuery) ||
                    s.Tags.Any(t => t.ToLowerInvariant().Contains(lowerQuery)))
                .ToList();

            return Task.FromResult(results);
        }

        /// <inheritdoc />
        public Task<List<SkillItem>> GetSkillsByCategoryAsync(string category, CancellationToken cancellationToken = default)
        {
            EnsureLoaded();
            
            var results = _registry.Skills
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(results);
        }

        /// <inheritdoc />
        public Task<SkillItem?> GetSkillAsync(string id, CancellationToken cancellationToken = default)
        {
            EnsureLoaded();
            
            var skill = _registry.Skills
                .FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(skill);
        }

        /// <inheritdoc />
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            // 静态客户端不自动刷新，可以加载本地文件
            await LoadFromFileAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // 静态数据始终可用
        }

        /// <summary>
        /// 加载内置示例数据
        /// </summary>
        private void LoadBuiltInData()
        {
            _registry = new SkillsRegistry
            {
                Version = "1.0.0",
                LastUpdated = DateTime.UtcNow,
                Source = "builtin",
                Skills = new List<SkillItem>
                {
                    new()
                    {
                        Id = "code-reviewer",
                        Name = "代码审查助手",
                        Description = "专业的代码审查 Skill，帮助发现潜在问题、安全漏洞和性能优化建议。支持多种编程语言。",
                        Author = "AIReady Team",
                        Category = SkillCategories.Development,
                        Tags = new() { "code-review", "security", "performance" },
                        Rating = 4.8,
                        InstallCount = 1250,
                        LastUpdated = new DateTime(2026, 3, 15)
                    },
                    new()
                    {
                        Id = "doc-writer",
                        Name = "文档生成器",
                        Description = "自动生成项目文档、API 文档和 README。支持多种文档格式和模板。",
                        Author = "OpenSource Community",
                        Category = SkillCategories.Development,
                        Tags = new() { "documentation", "api", "readme" },
                        Rating = 4.5,
                        InstallCount = 890,
                        LastUpdated = new DateTime(2026, 3, 10)
                    },
                    new()
                    {
                        Id = "sql-optimizer",
                        Name = "SQL 优化专家",
                        Description = "分析和优化 SQL 查询性能，提供索引建议和查询重写方案。",
                        Author = "DBA Tools",
                        Category = SkillCategories.DataAnalysis,
                        Tags = new() { "sql", "database", "optimization" },
                        Rating = 4.7,
                        InstallCount = 650,
                        LastUpdated = new DateTime(2026, 3, 8)
                    },
                    new()
                    {
                        Id = "blog-writer",
                        Name = "博客写作助手",
                        Description = "帮助撰写技术博客、文章和教程。提供结构建议和 SEO 优化。",
                        Author = "ContentAI",
                        Category = SkillCategories.ContentCreation,
                        Tags = new() { "writing", "blog", "seo" },
                        Rating = 4.6,
                        InstallCount = 2100,
                        LastUpdated = new DateTime(2026, 3, 12)
                    },
                    new()
                    {
                        Id = "shell-helper",
                        Name = "Shell 命令助手",
                        Description = "生成和解释 Shell 命令，帮助系统管理和自动化脚本编写。",
                        Author = "DevOps Tools",
                        Category = SkillCategories.SystemAdmin,
                        Tags = new() { "shell", "bash", "automation" },
                        Rating = 4.9,
                        InstallCount = 3200,
                        LastUpdated = new DateTime(2026, 3, 14)
                    },
                    new()
                    {
                        Id = "data-analyzer",
                        Name = "数据分析助手",
                        Description = "分析 CSV、JSON 等数据文件，生成统计报告和可视化建议。",
                        Author = "DataScience Team",
                        Category = SkillCategories.DataAnalysis,
                        Tags = new() { "data", "analysis", "csv", "json" },
                        Rating = 4.4,
                        InstallCount = 780,
                        LastUpdated = new DateTime(2026, 3, 5)
                    },
                    new()
                    {
                        Id = "ui-designer",
                        Name = "UI 设计助手",
                        Description = "提供 UI/UX 设计建议、颜色搭配和布局优化。",
                        Author = "DesignStudio",
                        Category = SkillCategories.Design,
                        Tags = new() { "ui", "ux", "design" },
                        Rating = 4.3,
                        InstallCount = 540,
                        LastUpdated = new DateTime(2026, 3, 1)
                    },
                    new()
                    {
                        Id = "meeting-notes",
                        Name = "会议记录整理",
                        Description = "自动整理会议纪要，提取行动项和关键决策。",
                        Author = "Productivity Tools",
                        Category = SkillCategories.Business,
                        Tags = new() { "meeting", "notes", "productivity" },
                        Rating = 4.7,
                        InstallCount = 1560,
                        LastUpdated = new DateTime(2026, 3, 13)
                    }
                }
            };
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        private async Task LoadFromFileAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (File.Exists(_dataPath))
                {
                    var json = await File.ReadAllTextAsync(_dataPath, cancellationToken);
                    var registry = JsonSerializer.Deserialize<SkillsRegistry>(json);
                    if (registry != null)
                    {
                        _registry = registry;
                        return;
                    }
                }
            }
            catch
            {
                // 文件加载失败，使用内置数据
            }

            LoadBuiltInData();
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        public async Task SaveToFileAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_registry, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_dataPath, json, cancellationToken);
            }
            catch
            {
                // 保存失败，忽略
            }
        }

        /// <summary>
        /// 确保数据已加载
        /// </summary>
        private void EnsureLoaded()
        {
            if (_registry.Skills.Count == 0)
            {
                LoadBuiltInData();
            }
        }

        /// <summary>
        /// 更新 Skills 数据（用于从其他源同步）
        /// </summary>
        public void UpdateSkills(List<SkillItem> skills, string source)
        {
            _registry.Skills = skills;
            _registry.Source = source;
            _registry.LastUpdated = DateTime.UtcNow;
            _ = SaveToFileAsync();
        }
    }
}
