# AI-Ready 技术蓝图

## 1. 技术选型总览

| 层级 | 技术选择 | 理由 |
|------|---------|------|
| **桌面端** | WinUI 3 (WASDK 1.8) | 原生性能、WinRT API、MSIX 分发、现代 Fluent UI |
| **移动端** | Android Native (Kotlin) | 后期开发，配合云算力监控场景 |
| **后端服务** | .NET Core 8/9 | 云原生、高性能、成熟的生态 |
| **云端 Agent** | Go 1.22+ | 单二进制、跨平台、轻量级、SSH 库成熟 |
| **数据存储** | SQLite (本地) + PostgreSQL (SaaS) | 本地零配置，云端高性能 |

---

## 2. 技术架构

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           AI-Ready 生态系统                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────┐      ┌─────────────────────┐                   │
│  │   Windows Desktop   │      │   Android Mobile    │                   │
│  │   (WinUI 3)         │      │   (Kotlin)          │                   │
│  │                     │      │                     │                   │
│  │  ┌───────────────┐  │      │  ┌───────────────┐  │                   │
│  │  │ Local Engine  │  │      │  │ Cloud Monitor │  │                   │
│  │  │ - Miniconda   │  │      │  │ - SSH Client  │  │                   │
│  │  │ - Docker Desktop│ │      │  │ - Status View │  │                   │
│  │  └───────────────┘  │      │  └───────────────┘  │                   │
│  │                     │      │                     │                   │
│  │  ┌───────────────┐  │      └──────────┬──────────┘                   │
│  │  │ Cloud Engine  │  │                 │                               │
│  │  │ - SSH.NET     │  │                 │                               │
│  │  │ - File Mgr    │  │                 │                               │
│  │  │ - Agent Ctrl  │  │                 │                               │
│  │  └───────────────┘  │                 │                               │
│  └──────────┬──────────┘                 │                               │
│             │                            │                               │
│             │    Config Sync (HTTPS)     │                               │
│             └──────────────┬─────────────┘                               │
│                            │                                            │
│                   ┌────────▼────────┐                                   │
│                   │  AI-Ready SaaS  │                                   │
│                   │  (.NET Core)    │                                   │
│                   │                 │                                   │
│                   │ - User/Auth     │                                   │
│                   │ - Config Sync   │                                   │
│                   │ - Content Push  │                                   │
│                   │ - Partner API   │                                   │
│                   └────────┬────────┘                                   │
│                            │                                            │
│         ┌──────────────────┼──────────────────┐                        │
│         │                  │                  │                        │
│  ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────┐                 │
│  │  Cloud GPU  │   │  Cloud GPU  │   │  Cloud GPU  │                 │
│  │  Server 1   │   │  Server 2   │   │  Server N   │                 │
│  │             │   │             │   │             │                 │
│  │ ┌─────────┐ │   │ ┌─────────┐ │   │ ┌─────────┐ │                 │
│  │ │Agent(Go)│ │   │ │Agent(Go)│ │   │ │Agent(Go)│ │                 │
│  │ └────┬────┘ │   │ └────┬────┘ │   │ └────┬────┘ │                 │
│  │      │      │   │      │      │   │      │      │                 │
│  │ ┌────▼────┐ │   │ ┌────▼────┐ │   │ ┌────▼────┐ │                 │
│  │ │ Docker  │ │   │ │ Docker  │ │   │ │ Docker  │ │                 │
│  │ │ Engine  │ │   │ │ Engine  │ │   │ │ Engine  │ │                 │
│  │ └────┬────┘ │   │ └────┬────┘ │   │ └────┬────┘ │                 │
│  │      │      │   │      │      │   │      │      │                 │
│  │ ┌────▼────┐ │   │ ┌────▼────┐ │   │ ┌────▼────┐ │                 │
│  │ │AI App   │ │   │ │AI App   │ │   │ │AI App   │ │                 │
│  │ │Containers│ │   │ │Containers│ │   │ │Containers│ │                 │
│  │ └─────────┘ │   │ └─────────┘ │   │ └─────────┘ │                 │
│  └─────────────┘   └─────────────┘   └─────────────┘                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 模块依赖关系

```
AIReady.Desktop (WinUI 3)
├── AIReady.Shared (.NET Standard 2.1)
│   ├── Models (DTOs, Entities)
│   ├── Contracts (Interfaces)
│   └── Utils (Shared utilities)
│
├── AIReady.Local.Core
│   ├── MinicondaService
│   ├── EnvironmentManager
│   ├── HardwareDetector
│   └── WorkflowInstaller
│
└── AIReady.Cloud.Core
    ├── SshService (基于 SSH.NET)
    ├── DockerRemoteService
    ├── FileTransferService
    └── AgentCommunicationService

AIReady.Service (.NET Core Web API)
├── UserModule
├── ConfigSyncModule
├── ContentModule
└── PartnerIntegrationModule

AIReady.Agent (Go)
├── SystemCollector (性能采集)
├── DockerManager (容器管理)
├── FileServer (文件服务)
└── TunnelServer (反向隧道)
```

---

## 3. 关键技术方案

### 3.1 本地 Miniconda 管理

```csharp
// 封装策略：调用 conda CLI，不重新实现
public interface IMinicondaService
{
    Task<bool> IsInstalledAsync();
    Task<InstallResult> InstallAsync(string installPath);
    Task<EnvInfo[]> ListEnvironmentsAsync();
    Task<bool> CreateEnvironmentAsync(string name, string pythonVersion);
    Task<bool> InstallPackageAsync(string envName, string package);
}
```

### 3.2 SSH 与文件管理

| 功能 | 库/工具 | 说明 |
|------|--------|------|
| SSH 连接 | [SSH.NET](https://github.com/sshnet/SSH.NET) | 成熟的 C# SSH 库 |
| SFTP 文件传输 | SSH.NET.SftpClient | 内置支持 |
| 虚拟磁盘 (V2) | WinFsp + SSHFS-Win | 后期集成 |
| 文件浏览 UI | WinUI 3 TreeView + 自定义 | 内置实现 |

### 3.3 云端 Agent 通信

```go
// Agent 架构 - 轻量单二进制
// 通过 SSH 隧道暴露 HTTP API，桌面端通过本地端口转发访问

package main

type Agent struct {
    SystemCollector  // 采集 GPU/CPU/内存
    DockerManager    // 调用本地 Docker API
    FileManager      // SFTP 服务端
    TunnelManager    // 反向隧道管理
}

// 启动时注册到桌面端
// Desktop <-SSH-> Server:22 <-localhost-> Agent:18080
```

### 3.4 Docker 远程管理

```csharp
// 通过 SSH 隧道转发 Docker TCP 端口
// Desktop -> SSH tunnel -> Server:2375 -> Docker Daemon

public interface IDockerRemoteService
{
    Task<Container[]> ListContainersAsync();
    Task<bool> DeployWorkflowAsync(WorkflowTemplate template);
    Task<Stream> GetContainerLogsAsync(string containerId);
}
```

---

## 4. 数据存储设计

### 4.1 本地存储 (SQLite)

```sql
-- 连接配置
CREATE TABLE Connections (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Host TEXT NOT NULL,
    Port INTEGER DEFAULT 22,
    Username TEXT NOT NULL,
    AuthType INTEGER, -- 0=Password, 1=Key
    EncryptedCredential BLOB,
    CreatedAt DATETIME
);

-- 本地环境
CREATE TABLE LocalEnvironments (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    PythonVersion TEXT,
    Path TEXT NOT NULL,
    WorkflowInstalled TEXT -- JSON
);

-- 设置
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
```

### 4.2 SaaS 存储 (PostgreSQL)

```sql
-- 用户与配置同步
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    Email TEXT UNIQUE,
    CreatedAt TIMESTAMP
);

CREATE TABLE CloudConfigs (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    ConfigData JSONB,
    LastModified TIMESTAMP
);

-- 内容推送
CREATE TABLE ContentItems (
    Id UUID PRIMARY KEY,
    Type TEXT, -- tutorial, news, workflow
    Title TEXT,
    Content TEXT,
    PublishedAt TIMESTAMP
);
```

---

## 5. 安全策略

### 5.1 本地安全
- SSH 密钥使用 Windows DPAPI 加密存储
- 敏感配置存储在 Windows Credential Manager
- 最小权限原则（不请求管理员权限除非必要）

### 5.2 通信安全
- 所有 SaaS 通信使用 HTTPS
- Agent 通信通过已建立的 SSH 加密隧道
- 无外部端口暴露（Agent 只监听 localhost）

---

## 6. 开源策略

- **License**: MIT License
- **代码仓库**: GitHub 公开仓库
- **SaaS 服务**: 部分功能需订阅（标记为 Pro 的功能）
- **贡献指南**: 欢迎社区贡献工作流模板和 Agent 功能
