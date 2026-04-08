#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIReady.Shared.Models.McpRegistry;

namespace AIReady.Shared.Services
{
    /// <summary>
    /// MCP Registry API 客户端实现
    /// </summary>
    public class McpRegistryApiClient : IMcpRegistryClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private const string DefaultBaseUrl = "https://registry.modelcontextprotocol.io/v0/";  // 末尾必须加 /，否则 Uri 拼接会替换掉 v0

        public McpRegistryApiClient(HttpClient? httpClient = null)
        {
            FileLogger.Log("McpRegistryApiClient 构造函数开始", "DEBUG");
            
            if (httpClient != null)
            {
                FileLogger.Log("使用传入的 HttpClient", "DEBUG");
                _httpClient = httpClient;
                // 确保 BaseAddress 已设置
                if (_httpClient.BaseAddress == null)
                {
                    FileLogger.Log("传入的 HttpClient 没有 BaseAddress，设置默认值", "DEBUG");
                    _httpClient.BaseAddress = new Uri(DefaultBaseUrl);
                }
            }
            else
            {
                try
                {
                    FileLogger.Log($"创建 HttpClient，BaseAddress: {DefaultBaseUrl}", "DEBUG");
                    _httpClient = new HttpClient();
                    _httpClient.BaseAddress = new Uri(DefaultBaseUrl);
                    _httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 设置默认请求头
                    _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "AIReady-Desktop/1.0");
                    FileLogger.Log($"HttpClient 创建完成，BaseAddress: {_httpClient.BaseAddress}", "DEBUG");
                }
                catch (Exception ex)
                {
                    FileLogger.LogException(ex, "McpRegistryApiClient 构造函数异常");
                    throw;
                }
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            FileLogger.Log("McpRegistryApiClient 构造函数完成", "DEBUG");
        }

        /// <inheritdoc />
        public async Task<McpRegistryResponse> GetServersAsync(
            int? limit = null,
            int? offset = null,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            FileLogger.Log("GetServersAsync 开始", "DEBUG");
            
            var queryParams = new List<string>();
            
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            if (offset.HasValue)
                queryParams.Add($"offset={offset.Value}");
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            var url = "servers";  // 不要以 / 开头，否则 Uri 会替换掉 BaseAddress 的路径
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            // 检查 BaseAddress
            var fullUrl = _httpClient.BaseAddress != null 
                ? new Uri(_httpClient.BaseAddress, url).ToString()
                : $"{DefaultBaseUrl}{url}";
            FileLogger.Log($"完整请求 URL: {fullUrl}", "DEBUG");
            FileLogger.Log($"BaseAddress: {_httpClient.BaseAddress}", "DEBUG");

            try
            {
                FileLogger.Log("发送 HTTP 请求", "DEBUG");
                var response = await _httpClient.GetAsync(url, cancellationToken);
                FileLogger.Log($"HTTP 响应状态: {response.StatusCode}", "DEBUG");
                response.EnsureSuccessStatusCode();

                FileLogger.Log("读取响应内容", "DEBUG");
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                FileLogger.Log($"响应内容长度: {content.Length}", "DEBUG");
                
                FileLogger.Log("反序列化 JSON", "DEBUG");
                var result = JsonSerializer.Deserialize<McpRegistryResponse>(content, _jsonOptions);
                FileLogger.Log($"反序列化完成，服务器数量: {result?.Servers.Count ?? 0}", "DEBUG");
                
                return result ?? new McpRegistryResponse();
            }
            catch (HttpRequestException ex)
            {
                FileLogger.LogException(ex, "GetServersAsync HttpRequestException");
                throw new McpRegistryException($"Failed to fetch MCP servers: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                FileLogger.LogException(ex, "GetServersAsync JsonException");
                throw new McpRegistryException($"Failed to parse MCP registry response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                FileLogger.LogException(ex, "GetServersAsync 未知异常");
                throw new McpRegistryException($"Unexpected error: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<McpServerEntry>> GetAllServersAsync(
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var allServers = new List<McpServerEntry>();
            string? nextCursor = null;
            const int pageSize = 100;

            do
            {
                // 计算 offset
                int? offset = string.IsNullOrEmpty(nextCursor) ? 0 : allServers.Count;

                var response = await GetServersAsync(
                    limit: pageSize,
                    offset: offset,
                    search: search,
                    cancellationToken: cancellationToken);

                allServers.AddRange(response.Servers);
                nextCursor = response.Metadata.NextCursor;

                // 安全检查：防止无限循环
                if (response.Servers.Count == 0)
                    break;

            } while (!string.IsNullOrEmpty(nextCursor) && !cancellationToken.IsCancellationRequested);

            return allServers;
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 秒超时
                
                var response = await _httpClient.GetAsync("/servers?limit=1", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (TaskCanceledException)
            {
                // 超时或取消
                return false;
            }
            catch (HttpRequestException)
            {
                // 网络错误
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// MCP Registry 异常
    /// </summary>
    public class McpRegistryException : Exception
    {
        public McpRegistryException(string message) : base(message) { }
        public McpRegistryException(string message, Exception innerException) : base(message, innerException) { }
    }
}
