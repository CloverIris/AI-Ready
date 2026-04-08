# AI-Ready Project Guide

## 项目概述

AI-Ready 是一款面向个人用户和云算力使用者的 AI 部署管理工具，解决本地 AI 环境配置复杂和云服务器使用门槛高的问题。

### 核心价值
- **零门槛本地部署**：像安装普通软件一样部署 Stable Diffusion、Ollama 等 AI 项目
- **一站式云算力管理**：通过桌面端轻松管理远程 Linux 服务器上的 AI 实例
- **环境可迁移**："末影箱"机制实现环境备份与跨服务器迁移
- **AI 生态集成**：Skills 商店、MCP 市场、API 调试器、AI 热点

---

## 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| Windows 桌面端 | WinUI 3 (Windows App SDK) | 1.8.260317003 |
| Build Tools | Microsoft.Windows.SDK.BuildTools | 10.0.28000.1721 |
| 目标框架 | .NET | 8.0-windows10.0.19041.0 |
| 共享库 | .NET | 8.0 |
| 后端服务 | .NET | 8.0 |
| 云端 Agent | Go | 1.22+ |
| 本地数据库 | SQLite | - |
| SaaS 数据库 | PostgreSQL | - |

**注意**: 已从 .NET Standard 2.1 迁移到 .NET 8，统一使用最新的 C# 12 特性。

---

## 项目结构

```
AIReady/
├── docs/                          # 文档
│   ├── prd/                       # 产品需求文档
│   ├── blueprint/                 # 技术蓝图
│   ├── architecture/              # 架构设计
│   │   ├── ARCHITECTURE.md        # 系统架构
│   │   ├── EXTERNAL_APIS.md       # 外部 API 集成
│   │   └── DATA_SOURCES.md        # 数据源架构
│   └── design/                    # UX/UI 设计
├── src/
│   ├── AIReady.Desktop/           # Windows 桌面端 (WinUI 3)
│   │   ├── Pages/                 # 页面
│   │   │   ├── HomePage.xaml      # 首页 - 硬件检测、快速入口
│   │   │   ├── LocalPage.xaml     # 本地 AI - 环境管理
│   │   │   ├── CloudPage.xaml     # 云端管理 - 服务器列表
│   │   │   ├── WorkflowsPage.xaml # 工作流 - AI 项目部署
│   │   │   ├── SkillsStorePage.xaml      # Skills 商店
│   │   │   ├── McpMarketplacePage.xaml   # MCP 市场
│   │   │   ├── ApiDebuggerPage.xaml      # API 调试器 (开发中)
│   │   │   ├── HelpPage.xaml      # 帮助
│   │   │   └── SettingsPage.xaml  # 设置
│   │   ├── MainWindow.xaml        # 主窗口
│   │   ├── MainWindow.xaml.cs     # 主窗口逻辑
│   │   ├── App.xaml               # 应用资源
│   │   ├── picture/               # 图片资源
│   │   └── Assets/                # 应用图标
│   ├── AIReady.Shared/            # 共享库 (.NET 8)
│   │   ├── Models/                # 数据模型
│   │   │   ├── ApiEndpoint.cs     # API 端点配置
│   │   │   ├── AppVersionInfo.cs  # 应用版本信息
│   │   │   ├── McpRegistryModels.cs      # MCP 注册表模型
│   │   │   ├── SkillModels.cs     # Skills 模型
│   │   │   ├── HardwareInfo.cs    # 硬件信息
│   │   │   └── ...
│   │   └── Services/              # 服务层
│   │       ├── McpRegistryApiClient.cs   # MCP API 客户端
│   │       ├── StaticSkillsClient.cs     # Skills 数据源
│   │       └── FileLogger.cs      # 文件日志
│   ├── AIReady.Service/           # SaaS 后端 (.NET 8)
│   └── AIReady.Agent/             # 云端 Agent (Go)
├── AGENTS.md                      # 本文件
└── README.md                      # 项目说明
```

---

## 页面清单

| 页面 | 文件 | 状态 | 说明 |
|------|------|------|------|
| 首页 | HomePage.xaml | 已实现 | 硬件检测、快速入口、资讯公告 |
| 本地 AI | LocalPage.xaml | 已实现 | Miniconda 管理、环境配置 |
| 云端管理 | CloudPage.xaml | 已实现 | SSH 连接、服务器管理 |
| 工作流 | WorkflowsPage.xaml | 已实现 | AI 项目部署 |
| Skills 商店 | SkillsStorePage.xaml | 已实现 | 静态数据源、8 个示例 Skills |
| MCP 市场 | McpMarketplacePage.xaml | 已实现 | 官方 Registry API 集成 |
| API 调试器 | ApiDebuggerPage.xaml | 框架完成 | 代理服务、审计、提醒（后端开发中） |
| AI 热点 | AiHotspotPage.xaml | 计划中 | mDNS 服务发现、邻居共享 |
| 帮助 | HelpPage.xaml | 已实现 | 帮助文档 |
| 设置 | SettingsPage.xaml | 已实现 | 应用设置 |

---

## 架构原则

### 本地/云端分离模式

采用 VS Code Remote-SSH 风格的设计哲学：

1. **单一上下文**：用户任何时候都清楚自己在操作本地还是哪台服务器
2. **无自动同步**：本地文件不会自动上传到云端，需要明确的"部署"动作
3. **状态分离**：本地环境和云端环境完全隔离
4. **连接即切换**：点击服务器 = 进入该服务器的专属工作区

```
+---------------------------------------------------------+
|                    AI-Ready Desktop                      |
+---------------------------------------------------------+
|  导航栏                                                   |
|  +-- 首页                                               |
|  +-- 本地 AI      <-- 本地上下文                        |
|  +-- 云端管理     <-- 云端上下文（服务器列表）          |
|  +-- 工作流                                             |
|  +-- Skills 商店                                        |
|  +-- MCP 市场                                           |
|  +-- API 调试器                                         |
|  +-- 帮助                                               |
|  +-- 设置                                               |
+---------------------------------------------------------+
```

---

## 开发规范

### 命名规范

- **命名空间**：`AIReady.Desktop`（已全部统一）
- **程序集名称**：`AIReady.Desktop`
- **RootNamespace**：`AIReady.Desktop`
- **共享库命名空间**：`AIReady.Shared`

### 代码规范

- 使用 `#nullable enable` 启用可空引用类型检查
- 页面类放在 `AIReady.Desktop.Pages` 命名空间下
- 共享模型放在 `AIReady.Shared.Models` 命名空间下
- 共享服务放在 `AIReady.Shared.Services` 命名空间下

### XAML 规范

**NavigationViewItem Icon**：使用 `SymbolIcon` 或 `FontIcon` 元素，不要直接使用字符串

```xml
<!-- 正确 -->
<NavigationViewItem>
    <NavigationViewItem.Icon>
        <FontIcon Glyph="&#xE753;"/>
    </NavigationViewItem.Icon>
</NavigationViewItem>

<!-- 错误（某些字符串如 "Cloud" 不是有效的 Symbol 值） -->
<NavigationViewItem Icon="Cloud"/>
```

**Button Icon**：WinUI 3 中 `Button.Icon` 不被支持，使用 `StackPanel`：

```xml
<Button>
    <StackPanel Orientation="Horizontal">
        <FontIcon Glyph="&#xE710;" Margin="0,0,8,0"/>
        <TextBlock Text="添加服务器"/>
    </StackPanel>
</Button>
```

**ToggleSwitch 事件**：使用 `Toggled` 而不是 `Click`：

```xml
<!-- 正确 -->
<ToggleSwitch Toggled="ToggleSwitch_Toggled"/>

<!-- 错误 -->
<ToggleSwitch Click="ToggleSwitch_Click"/>
```

---

## 构建配置

### 解决方案配置

| 配置 | 平台 | 用途 |
|------|------|------|
| Debug | x64 | 开发调试 |
| Release | x64 | 发布 |

### 启动配置

在 Visual Studio 的启动下拉框中选择：

- **AIReady.Desktop (Unpackaged)** - 直接运行，无需打包，启动更快
- **AIReady.Desktop (Packaged)** - 以 MSIX 包形式运行

### 项目属性

```xml
<WindowsPackageType>None</WindowsPackageType>  <!-- 默认 Unpackaged -->
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
```

---

## 窗口行为

### 当前实现

- **启动位置**：屏幕中央（DPI 感知）
- **窗口大小**：1000x600（基础大小，自动 DPI 缩放）
- **最大化按钮**：禁用
- **调整大小**：禁用
- **标题栏双击**：禁用（通过窗口子类化拦截 `WM_NCLBUTTONDBLCLK`）

### 版本标签

标题栏左侧根据构建配置显示不同标签：

- **Debug 模式**：`[debug]` `[dev0.1.0]` `[WinUI3 1.8]`
- **Release 模式**：`[v0.1.0]`

配置在 `AppVersionInfo.cs` 中修改。

### 关键代码

```csharp
// 禁用最大化
var presenter = appWindow.Presenter as OverlappedPresenter;
presenter.IsMaximizable = false;
presenter.IsResizable = false;

// 禁用标题栏双击（窗口子类化）
private const uint WM_NCLBUTTONDBLCLK = 0x00A3;
private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
{
    if (msg == WM_NCLBUTTONDBLCLK)
        return IntPtr.Zero;  // 拦截双击
    return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
}
```

---

## 已知问题与解决方案

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| NETSDK1022 重复 Page 项 | .NET SDK 自动包含 .xaml 文件 | 从 .csproj 中删除显式的 `<Page Include="..."/>` |
| WMC0011 Button.Icon | WinUI 3 不支持 Button.Icon | 使用 StackPanel 包含 FontIcon 和 TextBlock |
| WMC0011 ToggleSwitch.Click | WinUI 3 不支持 Click | 使用 Toggled 事件 |
| XamlParseException Cloud icon | "Cloud" 不是有效的 Symbol 枚举值 | 使用 `<FontIcon Glyph="&#xE753;"/>` |
| CS0246 IntPtr/Dictionary | 缺少 using 语句 | 添加 `using System;` 和 `using System.Collections.Generic;` |
| CS8632 可空警告 | 未启用可空上下文 | 添加 `#nullable enable` |
| ContentDialog 重复显示 | 同时触发多个对话框 | 使用静态标志 `_isDialogShowing` 防止重复 |

---

## 开发指导原则

### 技术文档优先

在实现任何功能前，**必须**先搜索并阅读相关官方文档：

1. **WinUI 3 控件和 API**
   - 优先查阅 [WinUI 3 官方文档](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
   - 了解控件的正确用法、事件、属性
   - 参考 [WinUI 3 Gallery](https://www.microsoft.com/store/productId/9P3JFQ3QW9NR) 示例

2. **Fluent Design 规范**
   - 遵循 [Fluent Design System](https://learn.microsoft.com/en-us/windows/apps/design/) 设计原则
   - 使用正确的主题资源（ThemeResource）而非硬编码颜色
   - 遵循间距、圆角、阴影规范

3. **C# / .NET 最佳实践**
   - 遵循 [C# 编码约定](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
   - 使用现代 C# 特性（模式匹配、记录类型、可空引用等）
   - 遵循 [NET 设计指南](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)

### NuGet 包选择

在实现功能前，**必须**搜索是否已有成熟的 NuGet 包：

```
1. 搜索 nuget.org 关键词
2. 评估：下载量、维护状态、许可证兼容性
3. 优先选择 Microsoft 官方包或社区广泛使用的包
4. 避免引入不必要的依赖
```

常用包参考：
- `Microsoft.WindowsAppSDK` - WinUI 3 核心
- `Microsoft.Extensions.Http` - HTTP 客户端工厂
- `System.Text.Json` - JSON 序列化
- `Microsoft.Data.Sqlite` - SQLite 数据库

### 文档一致性

**必须**尊重已有文档的设计决策：

1. **PRD 优先**：功能实现必须符合 `docs/prd/PRD.md` 定义的需求
2. **架构遵循**：代码结构必须符合 `docs/architecture/` 定义的架构
3. **设计规范**：UI 实现必须符合 `docs/design/UX_UI_GUIDELINES.md`
4. **API 集成**：外部 API 使用必须符合 `docs/architecture/EXTERNAL_APIS.md`

修改文档时：
- 如果实现与文档冲突，**先更新文档**再修改代码
- 保持文档与代码同步
- 重大变更需要记录决策原因

---

## 开发工作流

### 添加新页面

1. 在 `Pages/` 目录下创建 `.xaml` 和 `.xaml.cs` 文件
2. 命名空间：`AIReady.Desktop.Pages`
3. 在 `MainWindow.xaml.cs` 的 `_pages` 字典中添加页面实例
4. 在 `MainWindow.xaml` 的 NavigationView 中添加菜单项

### 添加图标资源

1. 图片放入 `picture/` 目录
2. 在 `.csproj` 中添加：
   ```xml
   <Content Include="picture\FileName.jpg">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </Content>
   ```

### 更新 PRD

添加新功能时，同步更新：
1. `docs/prd/PRD.md` - 产品需求文档
2. `README.md` - 项目说明
3. `AGENTS.md` - 开发者指南（本文件）

---

## 外部 API 集成

### 集成前检查清单

在集成任何外部 API 前，**必须**完成以下步骤：

1. **搜索官方文档**
   - API 官方文档网站
   - OpenAPI/Swagger 规范
   - 开发者指南和最佳实践

2. **评估 API 可用性**
   - 是否需要认证？认证方式？
   - 速率限制？
   - 是否有 SDK/客户端库？

3. **搜索现有 NuGet 包**
   - 是否有官方 SDK？
   - 社区 SDK 的维护状态如何？

4. **文档化**
   - 在 `docs/architecture/EXTERNAL_APIS.md` 添加 API 规范
   - 记录端点、认证、数据模型

### 已集成 API

#### MCP Registry API

- **端点**: `https://registry.modelcontextprotocol.io/v0/servers`
- **文档**: `docs/architecture/EXTERNAL_APIS.md`
- **状态**: 已集成

#### Skills 数据源

- **来源**: GitHub `anthropics/skills`
- **当前实现**: 静态数据源（8 个示例 Skills）
- **文档**: `docs/architecture/DATA_SOURCES.md`

---

## 参考资源

### WinUI 3 / Fluent Design

- [WinUI 3 官方文档](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [Fluent Design System](https://learn.microsoft.com/en-us/windows/apps/design/)
- [控件库参考](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls)
- [主题资源](https://learn.microsoft.com/en-us/windows/apps/design/style/xaml-theme-resources)
- [Segoe MDL2 Assets 图标](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font)
- [WinUI 3 Gallery](https://www.microsoft.com/store/productId/9P3JFQ3QW9NR)

### C# / .NET

- [C# 文档](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [C# 编码约定](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET 设计指南](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [.NET API 浏览器](https://learn.microsoft.com/en-us/dotnet/api/)

### NuGet 包搜索

- [NuGet.org](https://www.nuget.org/)
- [NuGet 趋势](https://nugettrends.com/)

### 项目特定

- [MCP Registry 文档](https://modelcontextprotocol.io/registry/about)
- [Anthropic Skills GitHub](https://github.com/anthropics/skills)

---

## 联系方式

项目维护者根据实际填写
