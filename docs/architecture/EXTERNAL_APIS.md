# 外部 API 集成文档

## 概述

本文档记录 AI-Ready 集成的外部数据源 API，包括 MCP Registry 和 Skills 数据源。

---

## MCP Registry API

### 基本信息

| 属性 | 值 |
|------|-----|
| **官方文档** | https://modelcontextprotocol.io/registry/about |
| **API 根地址** | `https://registry.modelcontextprotocol.io/v0` |
| **协议** | REST / HTTPS |
| **数据格式** | JSON |
| **认证** | 无需认证（公开 API） |

### 端点列表

#### 1. 获取 Servers 列表

```http
GET /servers?limit={limit}&offset={offset}&search={search}
```

**参数：**

| 参数 | 类型 | 必需 | 描述 |
|------|------|------|------|
| `limit` | integer | 否 | 每页数量（最大 100） |
| `offset` | integer | 否 | 分页偏移量 |
| `search` | string | 否 | 搜索关键词 |

**响应示例：**

```json
{
  "servers": [
    {
      "server": {
        "$schema": "https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json",
        "name": "agency.lona/trading",
        "description": "AI-powered trading strategy development: backtesting, market data, and portfolio analysis",
        "title": "Lona Trading",
        "version": "2.0.0",
        "websiteUrl": "https://lona.agency",
        "repository": {
          "url": "https://github.com/mindsightventures/lona",
          "source": "github",
          "id": "891584339",
          "subfolder": "packages/lona-mcp-server"
        },
        "icons": [
          {
            "src": "https://example.com/icon.png",
            "mimeType": "image/png",
            "sizes": ["96x96"]
          }
        ],
        "remotes": [
          {
            "type": "streamable-http",
            "url": "https://mcp.lona.agency/mcp"
          }
        ],
        "packages": [
          {
            "registryType": "npm",
            "identifier": "@lona/mcp-server",
            "version": "2.0.0",
            "transport": {
              "type": "stdio"
            },
            "environmentVariables": [
              {
                "name": "LONA_API_KEY",
                "description": "API key for authentication",
                "isRequired": true,
                "isSecret": true
              }
            ]
          }
        ]
      },
      "_meta": {
        "io.modelcontextprotocol.registry/official": {
          "status": "active",
          "statusChangedAt": "2026-02-24T00:07:27.525636Z",
          "publishedAt": "2026-02-24T00:07:27.525636Z",
          "updatedAt": "2026-02-24T00:07:27.525636Z",
          "isLatest": true
        }
      }
    }
  ],
  "metadata": {
    "nextCursor": "agency.lona/trading:2.0.0",
    "count": 20
  }
}
```

### 数据模型

#### McpServer

| 字段 | 类型 | 描述 |
|------|------|------|
| `name` | string | 唯一标识符（反向 DNS 格式，如 `io.github.user/server`） |
| `description` | string | 服务器描述 |
| `title` | string | 显示标题 |
| `version` | string | 版本号 |
| `websiteUrl` | string | 官方网站 URL |
| `repository` | Repository | 代码仓库信息 |
| `icons` | Icon[] | 图标列表 |
| `remotes` | Remote[] | 远程服务器配置 |
| `packages` | Package[] | 本地包配置 |

#### Repository

| 字段 | 类型 | 描述 |
|------|------|------|
| `url` | string | 仓库 URL |
| `source` | string | 来源类型（如 `github`） |
| `id` | string | 仓库 ID |
| `subfolder` | string | 子文件夹路径 |

#### Remote

| 字段 | 类型 | 描述 |
|------|------|------|
| `type` | string | 传输类型：`streamable-http`, `sse`, `stdio` |
| `url` | string | 服务器 URL |
| `headers` | Header[] | 请求头配置 |

#### Package

| 字段 | 类型 | 描述 |
|------|------|------|
| `registryType` | string | 注册表类型：`npm`, `pypi`, `oci` (Docker) |
| `identifier` | string | 包标识符 |
| `version` | string | 版本号 |
| `transport` | Transport | 传输配置 |
| `environmentVariables` | EnvVar[] | 环境变量配置 |

#### Meta

| 字段 | 类型 | 描述 |
|------|------|------|
| `status` | string | 状态：`active`, `deprecated`, `suspended` |
| `publishedAt` | datetime | 发布时间 |
| `updatedAt` | datetime | 更新时间 |
| `isLatest` | boolean | 是否最新版本 |

### 传输类型

| 类型 | 描述 |
|------|------|
| `stdio` | 标准输入输出（本地进程） |
| `sse` | Server-Sent Events |
| `streamable-http` | 流式 HTTP |

### 包注册表类型

| 类型 | 描述 |
|------|------|
| `npm` | Node.js/npm 包 |
| `pypi` | Python 包 |
| `oci` | OCI/Docker 镜像 |

---

## Skills 数据源

### 当前状况

**⚠️ 注意：** 目前 **没有官方统一的 Skills Registry API**。Skills 分散在多个来源：

### 数据来源选项

#### 选项 1: Anthropic 官方 Skills (GitHub)

- **URL**: https://github.com/anthropics/skills
- **格式**: GitHub 仓库（Markdown 文件）
- **获取方式**: GitHub API 或直接读取 raw 文件
- **结构**: 
  ```
  skills/
  ├── creative-design/
  ├── development/
  ├── enterprise/
  └── document/
  ```

#### 选项 2: LobeHub Skills Marketplace (第三方)

- **URL**: https://lobehub.com/skills
- **特点**: 社区驱动的 Skills 市场
- **API**: 未公开文档

#### 选项 3: Claude Code Marketplace

- **访问方式**: 通过 Claude Code CLI
- **命令**: `claude plugin marketplace list`
- **限制**: 需要 Claude Code 客户端

### 推荐实现策略

**阶段 1 - 静态数据源：**
- 创建本地 `skills-registry.json` 文件
- 手动维护热门 Skills 列表
- 定期从 GitHub 同步

**阶段 2 - 动态获取：**
- 实现 GitHub API 客户端获取 `anthropics/skills`
- 解析 SKILL.md 文件元数据

**阶段 3 - 聚合：**
- 聚合多个来源数据
- 提供统一的搜索和浏览界面

---

## 集成架构

```
┌─────────────────────────────────────────────────────────────┐
│                    AI-Ready Desktop                         │
├─────────────────────────────────────────────────────────────┤
│  Pages                                                      │
│  ├── SkillsStorePage.xaml      # Skills 商店 UI             │
│  └── McpMarketplacePage.xaml   # MCP 市场 UI                │
├─────────────────────────────────────────────────────────────┤
│  Services                                                   │
│  ├── ISkillsRegistryClient     # Skills 数据源接口          │
│  │   ├── StaticSkillsClient    # 静态 JSON 实现             │
│  │   └── GitHubSkillsClient    # GitHub API 实现            │
│  └── IMcpRegistryClient        # MCP Registry 接口          │
│      └── McpRegistryApiClient  # 官方 API 实现              │
├─────────────────────────────────────────────────────────────┤
│  Models                                                     │
│  ├── SkillItem.cs              # Skills 数据模型            │
│  └── McpServerItem.cs          # MCP Server 数据模型        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  External APIs                                              │
│  ├── https://registry.modelcontextprotocol.io/v0/servers    │
│  └── https://api.github.com/repos/anthropics/skills/...     │
└─────────────────────────────────────────────────────────────┘
```

---

## 更新策略

| 数据源 | 更新频率 | 缓存策略 |
|--------|---------|---------|
| MCP Registry | 启动时 + 每小时 | 内存缓存 1 小时 |
| Skills (GitHub) | 启动时 + 每天 | 本地文件缓存 24 小时 |

---

## 错误处理

### 网络错误

```csharp
// 实现指数退避重试
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

### 降级策略

- MCP Registry 不可用时：显示缓存数据或"离线模式"提示
- Skills 数据源不可用时：显示内置示例 Skills

---

## 参考链接

- [MCP Registry 官方文档](https://modelcontextprotocol.io/registry/about)
- [MCP Registry API 示例](https://nordicapis.com/getting-started-with-the-official-mcp-registry-api/)
- [Anthropic Skills GitHub](https://github.com/anthropics/skills)
- [MCP Servers GitHub](https://github.com/modelcontextprotocol/servers)
