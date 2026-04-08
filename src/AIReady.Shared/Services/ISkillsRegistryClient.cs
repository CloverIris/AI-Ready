#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.Skills;

namespace AIReady.Shared.Services
{
    /// <summary>
    /// Skills Registry 客户端接口
    /// </summary>
    public interface ISkillsRegistryClient
    {
        /// <summary>
        /// 获取所有 Skills
        /// </summary>
        Task<List<SkillItem>> GetSkillsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索 Skills
        /// </summary>
        Task<List<SkillItem>> SearchSkillsAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按分类获取 Skills
        /// </summary>
        Task<List<SkillItem>> GetSkillsByCategoryAsync(string category, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取 Skill 详情
        /// </summary>
        Task<SkillItem?> GetSkillAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新数据
        /// </summary>
        Task RefreshAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查是否可用
        /// </summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
