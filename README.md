# AI-Ready

> 让每个人都能轻松部署和使用开源 AI

AI-Ready 是一款面向个人用户和云算力使用者的 AI 部署管理工具，解决本地 AI 环境配置复杂和云服务器使用门槛高的问题。

## 🌟 核心功能

### 本地模式
- 🔍 **硬件检测**：自动评估你的电脑是否适合运行 AI
- 🐍 **环境管理**：可视化 Miniconda 管理，告别命令行
- 🚀 **一键安装**：Stable Diffusion、Ollama 等工作流开箱即用
- 📚 **新手引导**：在部署中学习 PATH、CUDA、VENV 等概念

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
│   ├── AIReady.Shared/            # 共享库 (.NET Standard)
│   ├── AIReady.Service/           # SaaS 后端 (.NET Core)
│   └── AIReady.Agent/             # 云端 Agent (Go)
└── README.md
```

## 🛠️ 技术栈

| 组件 | 技术 |
|------|------|
| Windows 桌面端 | WinUI 3 (WASDK 1.8) |
| Android 移动端 | Kotlin |
| 共享库 | .NET Standard 2.1 |
| SaaS 后端 | .NET Core 8/9 |
| 云端 Agent | Go 1.22+ |
| 本地数据库 | SQLite |
| SaaS 数据库 | PostgreSQL |

## 🚀 快速开始

### 构建桌面端
```bash
# 使用 Visual Studio 2022
# 1. 打开 src/AIReady.Desktop/AIReady.Desktop.sln
# 2. 选择 x64 架构
# 3. F5 运行
```

### 构建云端 Agent
```bash
cd src/AIReady.Agent
go build -o ai-ready-agent
```

## 📖 文档

- [产品需求文档 (PRD)](docs/prd/PRD.md)
- [技术蓝图](docs/blueprint/TECHNICAL_BLUEPRINT.md)
- [架构设计](docs/architecture/ARCHITECTURE.md)
- [UX/UI 设计指南](docs/design/UX_UI_GUIDELINES.md)

## 🤝 贡献

我们欢迎各种形式的贡献：
- 🐛 提交 Issue 报告问题
- 💡 提议新功能
- 🔧 提交 Pull Request
- 📖 改进文档

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE) 开源。

## 🙏 致谢

感谢以下开源项目：
- [SSH.NET](https://github.com/sshnet/SSH.NET) - SSH 连接库
- [Docker](https://www.docker.com/) - 容器化平台
- [Miniconda](https://docs.conda.io/en/latest/miniconda.html) - Python 环境管理

---

<p align="center">
  Made with ❤️ for AI Enthusiasts
</p>
