# AI-Ready 架构设计文档

## 1. 设计原则

1. **复用优先**：不重复造轮子，复用 Conda、Docker、成熟 SSH 库
2. **模块化**：核心逻辑与 UI 分离，便于测试和迭代
3. **渐进增强**：MVP 先走通核心路径，再迭代高级功能
4. **跨平台就绪**：Agent 设计时就考虑 Linux/Windows 双平台

---

## 2. 核心模块设计

### 2.1 AIReady.Shared (共享层)

```
AIReady.Shared/
├── Models/
│   ├── HardwareInfo.cs          # 硬件检测结果
│   ├── EnvironmentInfo.cs       # Python环境信息
│   ├── ConnectionInfo.cs        # SSH连接配置
│   ├── WorkflowTemplate.cs      # 工作流模板
│   └── ServerStatus.cs          # 服务器状态
├── Contracts/
│   ├── IMinicondaService.cs
│   ├── ISshService.cs
│   ├── IDockerService.cs
│   └── IFileTransferService.cs
└── Utils/
    ├── EncryptionHelper.cs      # DPAPI加密
    ├── JsonSerializer.cs
    └── PlatformDetector.cs
```

### 2.2 AIReady.Local.Core (本地引擎)

```
AIReady.Local.Core/
├── Hardware/
│   ├── HardwareDetector.cs      # GPU/CPU/内存检测
│   └── CompatibilityChecker.cs  # 兼容性评估
├── Miniconda/
│   ├── MinicondaInstaller.cs    # 静默安装
│   ├── EnvironmentManager.cs    # Conda环境管理
│   └── PackageInstaller.cs      # pip包安装
├── Environment/
│   └── PathManager.cs           # 环境变量管理
└── Workflows/
    ├── WorkflowRegistry.cs      # 模板注册表
    ├── WorkflowInstaller.cs     # 安装器
    └── LaunchManager.cs         # 启动管理
```

**关键类设计**：

```csharp
public class MinicondaManager : IMinicondaService
{
    private readonly string _installPath;
    private readonly ILogger<MinicondaManager> _logger;
    
    // 检测是否已安装
    public async Task<bool> IsInstalledAsync() { }
    
    // 静默安装到指定目录
    public async Task<InstallResult> InstallAsync(string installPath) { }
    
    // 创建环境
    public async Task<bool> CreateEnvironmentAsync(
        string name, 
        string pythonVersion,
        CancellationToken ct = default) { }
    
    // 执行conda命令的底层方法
    private async Task<ProcessResult> ExecuteCondaAsync(
        string arguments,
        CancellationToken ct) { }
}
```

### 2.3 AIReady.Cloud.Core (云端引擎)

```
AIReady.Cloud.Core/
├── Ssh/
│   ├── SshConnectionPool.cs     # 连接池管理
│   ├── SshClientWrapper.cs      # SSH.NET封装
│   └── SftpFileManager.cs       # SFTP文件操作
├── Agent/
│   ├── AgentDeployer.cs         # Agent自动部署
│   ├── AgentClient.cs           # Agent通信客户端
│   └── AgentHealthMonitor.cs    # 健康检查
├── Docker/
│   ├── DockerTunnel.cs          # SSH隧道创建
│   └── DockerRemoteClient.cs    # Docker远程客户端
└── Files/
    └── EnderChestManager.cs     # 末影箱管理器
```

**Agent 部署流程**：

```
[Desktop] --SSH连接--> [Cloud Server]
    |
    |-- 检查系统架构 (amd64/arm64)
    |
    |-- 上传对应版本的 ai-ready-agent
    |   (通过 SFTP 上传到 /tmp/ai-ready-agent)
    |
    |-- 执行安装脚本
    |   - 检查/安装 Docker
    |   - 将 Agent 移动到 /opt/ai-ready/
    |   - 创建 systemd 服务 (可选)
    |
    |-- 建立反向隧道
    |   Desktop:localhost:random_port <-> Server:localhost:18080
    |
    |-- Agent API 可用
        GET  /api/v1/system/info
        GET  /api/v1/containers
        POST /api/v1/containers/deploy
        GET  /api/v1/files/{path}
```

### 2.4 AIReady.Agent (Go 实现)

```go
// main.go
package main

import (
    "context"
    "log"
    "net/http"
)

type Agent struct {
    config    *Config
    collector *SystemCollector
    docker    *DockerManager
    files     *FileServer
    tunnel    *TunnelManager
}

func main() {
    agent := NewAgent(LoadConfig())
    
    router := http.NewServeMux()
    router.HandleFunc("/api/v1/system/info", agent.handleSystemInfo)
    router.HandleFunc("/api/v1/containers", agent.handleContainers)
    router.HandleFunc("/api/v1/containers/deploy", agent.handleDeploy)
    router.HandleFunc("/api/v1/files/", agent.handleFiles)
    
    // 只监听 localhost，通过 SSH 隧道暴露
    log.Fatal(http.ListenAndServe("127.0.0.1:18080", router))
}
```

---

## 3. 工作流模板系统

### 3.1 模板定义

```yaml
# workflows/ollama-chatbox.yml
name: "Ollama + ChatBox"
description: "本地大语言模型聊天环境"
category: "llm"
requirements:
  minVram: 4
  recommendedVram: 8

local:
  type: "conda"
  pythonVersion: "3.11"
  packages:
    - ollama
  installScript: |
    pip install ollama
    # 下载默认模型
    ollama pull llama3.2
  launchCommand: "ollama serve"
  webUiPort: 11434

cloud:
  type: "docker"
  composeFile: |
    version: '3.8'
    services:
      ollama:
        image: ollama/ollama:latest
        volumes:
          - ollama-data:/root/.ollama
        ports:
          - "11434:11434"
        deploy:
          resources:
            reservations:
              devices:
                - driver: nvidia
                  count: 1
                  capabilities: [gpu]
      
      open-webui:
        image: ghcr.io/open-webui/open-webui:main
        ports:
          - "8080:8080"
        environment:
          - OLLAMA_BASE_URL=http://ollama:11434
        depends_on:
          - ollama
    
    volumes:
      ollama-data:
```

### 3.2 模板注册表

```csharp
public class WorkflowRegistry
{
    private readonly List<WorkflowTemplate> _templates;
    
    // 从嵌入资源/远程加载模板
    public async Task LoadTemplatesAsync() { }
    
    // 根据硬件推荐合适的工作流
    public IEnumerable<WorkflowTemplate> GetRecommended(HardwareInfo hardware) { }
    
    // 安装指定工作流
    public async Task<InstallResult> InstallAsync(
        string workflowId, 
        InstallTarget target) { }
}
```

---

## 4. 数据流设计

### 4.1 本地安装流程

```
[用户选择工作流]
    ↓
[HardwareDetector] 检查硬件兼容性
    ↓
[MinicondaManager] 确保 Conda 已安装
    ↓
[EnvironmentManager] 创建隔离环境
    ↓
[WorkflowInstaller] 执行安装脚本
    ↓
[LaunchManager] 启动服务
    ↓
[UI] 显示就绪状态，提供访问链接
```

### 4.2 云端部署流程

```
[用户添加服务器]
    ↓
[SshClient] 测试连接
    ↓
[AgentDeployer] 上传并启动 Agent
    ↓
[AgentClient] 获取系统信息
    ↓
[DockerRemoteClient] 部署工作流
    ↓
[TunnelManager] 建立端口转发
    ↓
[UI] 显示可访问的本地端口
```

---

## 5. 错误处理策略

| 层级 | 错误类型 | 处理方式 |
|------|---------|---------|
| 核心服务 | 业务异常 | 自定义异常类型，携带错误码 |
| 外部调用 | 网络/进程 | 重试+指数退避，最终抛给上层 |
| UI 层 | 用户可见 | 友好提示，提供解决方案链接 |

```csharp
// 自定义异常
public class AIReadyException : Exception
{
    public ErrorCode Code { get; }
    public string HelpLink { get; }
}

public enum ErrorCode
{
    CondaNotFound,
    InsufficientVram,
    SshConnectionFailed,
    AgentDeployFailed,
    DockerNotAvailable,
    // ...
}
```

---

## 6. 日志与诊断

```csharp
// 结构化日志
public interface IDiagnosticsService
{
    void LogOperation(string operation, Dictionary<string, object> context);
    void LogError(Exception ex, ErrorCode code);
    Task<string> ExportDiagnosticsAsync(); // 导出诊断包
}

// 日志输出：
// - 本地：Serilog -> 文件 + ETW
// - Agent：标准输出，由 systemd/docker 收集
```

---

## 7. 测试策略

| 类型 | 范围 | 工具 |
|------|------|------|
| 单元测试 | 核心服务类 | xUnit + Moq |
| 集成测试 | 数据库、外部服务 | TestContainers |
| UI 测试 | WinUI 界面 | WinAppDriver |
| E2E 测试 | 完整流程 | 自动化脚本 |

---

## 8. 扩展点

| 扩展 | 机制 | 示例 |
|------|------|------|
| 新工作流 | YAML 模板 | 用户自定义 workflow.yml |
| 新云厂商 | 实现 ICloudProvider | AutoDLProvider, HengyuanProvider |
| 新功能模块 | 插件系统 | 后期支持 DLL 插件 |
