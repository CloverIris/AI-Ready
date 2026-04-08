# AI-Ready

> 让每个人都能轻松部署和使用开源 AI

AI-Ready 是一款面向个人用户和云算力使用者的 AI 部署管理工具，解决本地 AI 环境配置复杂和云服务器使用门槛高的问题。

## 🌟 核心功能

### 本地模式
- 🔍 **硬件检测**：自动评估你的电脑是否适合运行 AI
- 🐍 **环境管理**：可视化 Miniconda 管理，告别命令行
- 🚀 **一键安装**：Stable Diffusion、Ollama 等工作流开箱即用
- 📚 **新手引导**：在部署中学习 PATH、CUDA、VENV 等概念

### AI 生态
- 🎁 **Skills 商店**：浏览和管理 AI Skills（预配置提示词模板）
- 🔌 **MCP 市场**：集成官方 MCP Registry，发现 Model Context Protocol 服务器
- 🧪 **API 调试器**：本地 API 代理、调试、审计、过期提醒一站式管理
- 🔥 **AI 热点**：局域网内共享 API，自动发现邻居节点

### 云端模式
- ☁️ **多服务器管理**：轻松管理 AutoDL、恒源云等平台的服务器
- 🐳 **Docker 部署**：通过 Agent 一键部署云端 AI 实例
- 📦 **末影箱**：VS Code 式的远程文件管理，环境可迁移
- 🔗 **网络透传**：通过 SSH 隧道解决云服务器联网限制

## 📦 项目结构

```
AIReady/
├── docs/                          # 文档
│   ├── prd/                       # 产品需求文档
│   ├── blueprint/                 # 技术蓝图
│   ├── architecture/              # 架构设计
│   └── design/                    # UX/UI 设计
├── src/
│   ├── AIReady.Desktop/           # Windows 桌面端 (WinUI 3)
│   │   ├── Pages/                 # 页面
│   │   │   ├── HomePage.xaml      # 首页
│   │   │   ├── LocalPage.xaml     # 本地 AI
│   │   │   ├── CloudPage.xaml     # 云端管理
│   │   │   ├── WorkflowsPage.xaml # 工作流
│   │   │   ├── SkillsStorePage.xaml      # Skills 商店
│   │   │   ├── McpMarketplacePage.xaml   # MCP 市场
│   │   │   ├── ApiDebuggerPage.xaml      # API 调试器
│   │   │   ├── HelpPage.xaml      # 帮助
│   │   │   └── SettingsPage.xaml  # 设置
│   │   ├── MainWindow.xaml        # 主窗口
│   │   └── Assets/                # 应用资源
│   ├── AIReady.Shared/            # 共享库 (.NET 8)
│   │   ├── Models/                # 数据模型
│   │   │   ├── ApiEndpoint.cs     # API 端点
│   │   │   ├── McpRegistryModels.cs      # MCP 注册表
│   │   │   ├── SkillModels.cs     # Skills
│   │   │   └── ...
│   │   └── Services/              # 服务层
│   │       ├── McpRegistryApiClient.cs   # MCP API 客户端
│   │       ├── StaticSkillsClient.cs     # Skills 数据源
│   │       └── FileLogger.cs      # 文件日志
│   ├── AIReady.Service/           # SaaS 后端 (.NET 8)
│   └── AIReady.Agent/             # 云端 Agent (Go)
└── README.md
```

## 🛠️ 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| Windows 桌面端 | WinUI 3 (WASDK) | 1.8+ |
| 共享库 | .NET | 8.0 |
| SaaS 后端 | .NET | 8.0 |
| 云端 Agent | Go | 1.22+ |
| 本地数据库 | SQLite | - |
| SaaS 数据库 | PostgreSQL | - |

## 🚀 快速开始

### 环境要求
- Windows 10 21H2+ / Windows 11
- Visual Studio 2022 或更高版本
- .NET 8.0 SDK
- Windows App SDK 1.8+

### 构建桌面端
```bash
# 使用 Visual Studio 2022
# 1. 打开 AIReady.sln
# 2. 选择 x64 架构
# 3. F5 运行（选择 AIReady.Desktop (Unpackaged)）
```

### 构建云端 Agent
```bash
cd src/AIReady.Agent
go build -o ai-ready-agent
```

## 🏷️ 版本标识

应用标题栏会根据构建配置显示不同的版本标签：

**Debug 模式**：
- `[debug]` - 调试构建
- `[dev0.1.0]` - 版本号
- `[WinUI3 1.8]` - WinUI 版本

**Release 模式**：
- `[v0.1.0]` - 简洁版本号

版本信息定义在 `src/AIReady.Shared/Models/AppVersionInfo.cs`

## 📖 文档

- [产品需求文档 (PRD)](docs/prd/PRD.md)
- [技术蓝图](docs/blueprint/TECHNICAL_BLUEPRINT.md)
- [架构设计](docs/architecture/ARCHITECTURE.md)
- [外部 API 集成](docs/architecture/EXTERNAL_APIS.md)
- [数据源架构](docs/architecture/DATA_SOURCES.md)
- [UX/UI 设计指南](docs/design/UX_UI_GUIDELINES.md)
- [开发者指南](AGENTS.md)

## 🗺️ 路线图

### 已实现 ✅
- [x] 硬件检测与兼容性报告
- [x] Miniconda 环境管理
- [x] Skills 商店（静态数据源）
- [x] MCP 市场（官方 Registry API 集成）
- [x] API 调试器（UI 框架）

### 进行中 🚧
- [ ] API 调试器后端服务（HTTP 代理、健康检测）
- [ ] AI 热点页面（mDNS 服务发现）
- [ ] 工作流一键安装

### 计划中 📋
- [ ] Android 移动端
- [ ] 云厂商合作集成
- [ ] 环境迁移功能

## 🤝 贡献

我们欢迎各种形式的贡献：
- 🐛 提交 Issue 报告问题
- 💡 提议新功能
- 🔧 提交 Pull Request
- 📖 改进文档

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 🙏 致谢

感谢以下开源项目和技术：
- [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml) - Windows UI 框架
- [MCP](https://modelcontextprotocol.io/) - Model Context Protocol
- [Anthropic Skills](https://github.com/anthropics/skills) - AI Skills 参考
- [SSH.NET](https://github.com/sshnet/SSH.NET) - SSH 连接库
- [Docker](https://www.docker.com/) - 容器化平台
- [Miniconda](https://docs.conda.io/en/latest/miniconda.html) - Python 环境管理

---

<p align="center">
  Made with ❤️ for AI Enthusiasts
</p>
