# AI-Ready Project Guide

## 项目概述

AI-Ready 是一款面向个人用户和云算力使用者的 AI 部署管理工具，解决本地 AI 环境配置复杂和云服务器使用门槛高的问题。

### 核心价值
- **零门槛本地部署**：像安装普通软件一样部署 Stable Diffusion、Ollama 等 AI 项目
- **一站式云算力管理**：通过桌面端轻松管理远程 Linux 服务器上的 AI 实例
- **环境可迁移**："末影箱"机制实现环境备份与跨服务器迁移

---

## 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| Windows 桌面端 | WinUI 3 (Windows App SDK) | 1.8.260317003 |
| Build Tools | Microsoft.Windows.SDK.BuildTools | 10.0.28000.1721 |
| 目标框架 | .NET | 8.0-windows10.0.19041.0 |
| 后端服务 | .NET Core | 8.0 |
| 云端 Agent | Go | 1.22+ |
| 本地数据库 | SQLite | - |
| SaaS 数据库 | PostgreSQL | - |

---

## 项目结构

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
│   │   ├── picture/               # 图片资源
│   │   └── Assets/                # 应用图标
│   ├── AIReady.Shared/            # 共享库 (.NET 8.0)
│   ├── AIReady.Service/           # SaaS 后端 (.NET Core)
│   └── AIReady.Agent/             # 云端 Agent (Go)
└── AGENTS.md                      # 本文件
```

---

## 架构原则

### 本地/云端分离模式

采用 VS Code Remote-SSH 风格的设计哲学：

1. **单一上下文**：用户任何时候都清楚自己在操作本地还是哪台服务器
2. **无自动同步**：本地文件不会自动上传到云端，需要明确的"部署"动作
3. **状态分离**：本地环境和云端环境完全隔离
4. **连接即切换**：点击服务器 = 进入该服务器的专属工作区

```
┌─────────────────────────────────────────────────────────┐
│                    AI-Ready Desktop                      │
├─────────────────────────────────────────────────────────┤
│  导航栏                                                  │
│  ├── 🏠 首页                                            │
│  ├── 💻 本地 AI      ← 本地上下文                        │
│  ├── ☁️ 云端管理     ← 云端上下文（服务器列表）           │
│  ├── 📚 工作流                                          │
│  └── ⚙️ 设置                                            │
└─────────────────────────────────────────────────────────┘
```

---

## 开发规范

### 命名规范

- **命名空间**：`AIReady.Desktop`（已全部统一）
- **程序集名称**：`AIReady.Desktop`
- **RootNamespace**：`AIReady.Desktop`

### 代码规范

- 使用 `#nullable enable` 启用可空引用类型检查
- 页面类放在 `AIReady.Desktop.Pages` 命名空间下

### XAML 规范

- **NavigationViewItem Icon**：使用 `SymbolIcon` 或 `FontIcon` 元素，不要直接使用字符串
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

- **Button Icon**：WinUI 3 中 `Button.Icon` 不被支持，使用 `StackPanel`：
  ```xml
  <Button>
      <StackPanel Orientation="Horizontal">
          <FontIcon Glyph="&#xE710;" Margin="0,0,8,0"/>
          <TextBlock Text="添加服务器"/>
      </StackPanel>
  </Button>
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
| `NETSDK1022` 重复 Page 项 | .NET SDK 自动包含 .xaml 文件 | 从 .csproj 中删除显式的 `<Page Include="..."/>` |
| `WMC0011` Button.Icon | WinUI 3 不支持 Button.Icon | 使用 StackPanel 包含 FontIcon 和 TextBlock |
| `XamlParseException` Cloud icon | "Cloud" 不是有效的 Symbol 枚举值 | 使用 `<FontIcon Glyph="&#xE753;"/>` |
| `CS0246` IntPtr/Dictionary | 缺少 using 语句 | 添加 `using System;` 和 `using System.Collections.Generic;` |
| `CS8632` 可空警告 | 未启用可空上下文 | 添加 `#nullable enable` |

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

---

## 参考资源

- [WinUI 3 NavigationViewItem.Icon](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.navigationviewitem.icon)
- [Segoe MDL2 Assets 图标列表](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font)

---

## 联系方式

项目维护者根据实际填写
