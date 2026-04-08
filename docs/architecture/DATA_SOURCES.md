# 外部数据源集成架构

## 概述

本文档说明 AI-Ready 如何集成外部 Skills 和 MCP 数据源。

---

## MCP Registry (官方 API)

### 数据源信息

| 属性 | 值 |
|------|-----|
| **名称** | MCP Registry |
| **维护方** | Anthropic, GitHub, PulseMCP, Microsoft |
| **API 地址** | `https://registry.modelcontextprotocol.io/v0/servers` |
| **协议** | REST / HTTPS |
| **认证** | 无需认证 |

### API 端点

```
GET /v0/servers?limit={limit}&offset={offset}&search={search}
```

### 响应结构

```json
{
  "servers": [
    {
      "server": {
        "name": "io.github.user/server-name",
        "description": "Server description",
        "version": "1.0.0",
        "websiteUrl": "https://example.com",
        "repository": {
          "url": "https://github.com/user/repo",
          "source": "github"
        },
        "icons": [{ "src": "https://...", "mimeType": "image/png" }],
        "remotes": [{ "type": "streamable-http", "url": "https://..." }],
        "packages": [{
          "registryType": "npm",
          "identifier": "@scope/package",
          "version": "1.0.0",
          "transport": { "type": "stdio" }
        }]
      },
      "_meta": {
        "io.modelcontextprotocol.registry/official": {
          "status": "active",
          "publishedAt": "2026-01-01T00:00:00Z",
          "isLatest": true
        }
      }
    }
  ],
  "metadata": {
    "nextCursor": "...",
    "count": 20
  }
}
```

### 代码实现

| 文件 | 说明 |
|------|------|
| `McpRegistryModels.cs` | 数据模型 |
| `IMcpRegistryClient.cs` | 接口定义 |
| `McpRegistryApiClient.cs` | API 客户端实现 |
| `McpMarketplacePage.xaml` | 市场页面 UI |
| `McpMarketplacePage.xaml.cs` | 页面逻辑 |

---

## Skills 数据源

### 当前状况

**⚠️ 重要：** 目前没有官方统一的 Skills Registry API。

### 可用数据源

#### 1. Anthropic 官方 Skills (GitHub)

- **仓库**: https://github.com/anthropics/skills
- **格式**: Markdown 文件 (SKILL.md)
- **结构**: 按分类组织的目录

```
skills/
├── creative-design/    # 创意设计
├── development/        # 开发技术
├── enterprise/         # 企业办公
└── document/           # 文档处理
```

#### 2. 当前实现

使用 **静态数据源** 作为临时方案：

| 文件 | 说明 |
|------|------|
| `SkillModels.cs` | 数据模型 |
| `ISkillsRegistryClient.cs` | 接口定义 |
| `StaticSkillsClient.cs` | 静态数据源实现 |
| `SkillsStorePage.xaml` | 商店页面 UI |
| `SkillsStorePage.xaml.cs` | 页面逻辑 |

### 内置示例 Skills

| ID | 名称 | 分类 | 评分 |
|----|------|------|------|
| code-reviewer | 代码审查助手 | 编程开发 | 4.8 |
| doc-writer | 文档生成器 | 编程开发 | 4.5 |
| sql-optimizer | SQL 优化专家 | 数据分析 | 4.7 |
| blog-writer | 博客写作助手 | 内容创作 | 4.6 |
| shell-helper | Shell 命令助手 | 系统管理 | 4.9 |
| data-analyzer | 数据分析助手 | 数据分析 | 4.4 |
| ui-designer | UI 设计助手 | 设计创意 | 4.3 |
| meeting-notes | 会议记录整理 | 商业办公 | 4.7 |

### 未来集成方案

**阶段 1 - 静态数据（当前）**
- ✅ 内置示例 Skills
- ✅ 本地 JSON 缓存

**阶段 2 - GitHub 集成（计划）**
- 使用 GitHub API 获取 `anthropics/skills`
- 解析 SKILL.md 元数据
- 自动同步更新

**阶段 3 - 聚合市场（未来）**
- 聚合多个来源：
  - Anthropic 官方 Skills
  - LobeHub Skills Marketplace
  - 社区贡献 Skills

---

## 架构图

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AI-Ready Desktop                            │
├─────────────────────────────────────────────────────────────────────┤
│  UI Layer                                                           │
│  ├── McpMarketplacePage.xaml   ─────┐                               │
│  ├── McpMarketplacePage.xaml.cs     │  ← 实时 API 数据              │
│  ├── SkillsStorePage.xaml           │                               │
│  └── SkillsStorePage.xaml.cs ──────┘  ← 本地/静态数据              │
├─────────────────────────────────────────────────────────────────────┤
│  Service Layer                                                      │
│  ├── IMcpRegistryClient          ─────────┐                         │
│  │   └── McpRegistryApiClient              │  ← HTTP 客户端         │
│  │                                         │    registry.model...   │
│  └── ISkillsRegistryClient       ─────────┘                         │
│      └── StaticSkillsClient          ← 本地 JSON/内置数据           │
├─────────────────────────────────────────────────────────────────────┤
│  Model Layer                                                        │
│  ├── McpRegistryModels.cs   (Server, Remote, Package...)            │
│  └── SkillModels.cs         (SkillItem, SkillsRegistry...)          │
└─────────────────────────────────────────────────────────────────────┘
                               │
           ┌───────────────────┴───────────────────┐
           ▼                                           ▼
┌──────────────────────────────┐        ┌─────────────────────────────┐
│  MCP Registry API            │        │  Skills Data Sources        │
│  registry.modelcontext...    │        │                             │
│                              │        │  • 内置示例数据 (当前)       │
│  REST API / JSON             │        │  • 本地 JSON 缓存           │
│                              │        │  • GitHub anthropic/skills │
└──────────────────────────────┘        │    (计划中)                 │
                                        └─────────────────────────────┘
```

---

## 数据流

### MCP Registry 数据流

```
┌──────────────┐    HTTP GET     ┌──────────────┐    JSON Deserialize    ┌──────────────┐
│   UI Page    │ ───────────────> │  API Client  │ ─────────────────────> │   UI Bind    │
│              │                  │              │                        │              │
│  Load Data   │ <─────────────── │  GetServers  │ <───────────────────── │  ViewModel   │
└──────────────┘    Update UI     └──────────────┘    Convert Model       └──────────────┘
```

### Skills 数据流

```
┌──────────────┐    Load         ┌──────────────┐    Convert             ┌──────────────┐
│   UI Page    │ ───────────────> │ StaticClient │ ─────────────────────> │   UI Bind    │
│              │                  │              │                        │              │
│  Load Data   │ <─────────────── │  GetSkills   │ <───────────────────── │  ViewModel   │
└──────────────┘    Update UI     └──────────────┘    Return List          └──────────────┘
                              │
                              ├──> 内置示例数据 (首次)
                              └──> 本地 JSON 缓存 (后续)
```

---

## 错误处理

### 网络错误

```csharp
try {
    var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
} catch (HttpRequestException ex) {
    // 显示错误提示
    // 使用缓存数据
}
```

### 降级策略

| 场景 | 处理方案 |
|------|---------|
| MCP Registry 不可用 | 显示错误 + 禁用搜索 |
| Skills 数据源不可用 | 使用内置示例数据 |
| 网络超时 | 3 次重试 + 错误提示 |

---

## 参考链接

- [MCP Registry 文档](https://modelcontextprotocol.io/registry/about)
- [MCP Registry API 指南](https://nordicapis.com/getting-started-with-the-official-mcp-registry-api/)
- [Anthropic Skills GitHub](https://github.com/anthropics/skills)
- [MCP Servers GitHub](https://github.com/modelcontextprotocol/servers)
