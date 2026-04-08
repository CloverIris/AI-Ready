#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.McpRegistry;

namespace AIReady.Shared.Services
{
    /// <summary>
    /// MCP Registry API 客户端接口
    /// </summary>
    public interface IMcpRegistryClient
    {
        /// <summary>
        /// 获取 MCP Servers 列表
        /// </summary>
        /// <param name="limit">每页数量（最大 100）</param>
        /// <param name="offset">分页偏移量</param>
        /// <param name="search">搜索关键词</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>MCP Registry 响应</returns>
        Task<McpRegistryResponse> GetServersAsync(
            int? limit = null,
            int? offset = null,
            string? search = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有 Servers（自动分页）
        /// </summary>
        /// <param name="search">搜索关键词</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有 Server 列表</returns>
        Task<List<McpServerEntry>> GetAllServersAsync(
            string? search = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查 API 是否可用
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否可用</returns>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
