#nullable enable

using System.Diagnostics;
using System.Text.RegularExpressions;
using AIReady.Local.Core.Miniconda;
using AIReady.Shared.Models;

namespace AIReady.Local.Core.Workflows;

/// <summary>
/// Installs workflow instances locally
/// </summary>
public class WorkflowInstaller
{
    private readonly MinicondaManager _minicondaManager;
    private readonly string _installBasePath;

    public WorkflowInstaller(MinicondaManager minicondaManager)
    {
        _minicondaManager = minicondaManager;
        _installBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIReady", "workflows");
    }

    /// <summary>
    /// Gets the base installation path
    /// </summary>
    public string InstallBasePath => _installBasePath;

    /// <summary>
    /// Installs a workflow locally
    /// </summary>
    public async Task<WorkflowInstance> InstallAsync(
        WorkflowTemplate template, 
        string? customInstallPath = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (template.Local == null)
        {
            throw new InvalidOperationException("Template does not support local installation");
        }

        var instance = new WorkflowInstance
        {
            TemplateId = template.Id,
            Name = template.Name,
            Target = InstallTarget.Local,
            Status = InstanceStatus.Installing,
            InstallPath = customInstallPath ?? Path.Combine(_installBasePath, template.Id),
            InstalledAt = DateTime.Now
        };

        try
        {
            // Ensure Miniconda is installed
            progress?.Report("检查 Miniconda...");
            if (!await _minicondaManager.IsInstalledAsync(cancellationToken))
            {
                throw new InvalidOperationException("Miniconda is not installed. Please install it first.");
            }

            // Create installation directory
            Directory.CreateDirectory(instance.InstallPath);

            // Create conda environment
            var envName = $"ai-ready-{template.Id}";
            progress?.Report($"创建 Python 环境 '{envName}'...");
            
            var pythonVersion = template.Local.PythonVersion ?? "3.10";
            await _minicondaManager.CreateEnvironmentAsync(envName, pythonVersion, progress, cancellationToken);

            // Install packages if specified
            if (template.Local.Packages?.Any() == true)
            {
                progress?.Report("安装依赖包...");
                await _minicondaManager.InstallPackagesAsync(envName, template.Local.Packages, progress, cancellationToken);
            }

            // Run install script if specified
            if (!string.IsNullOrEmpty(template.Local.InstallScript))
            {
                progress?.Report("执行安装脚本...");
                await RunInstallScriptAsync(envName, template.Local.InstallScript, instance.InstallPath, progress, cancellationToken);
            }

            // Save instance metadata
            await SaveInstanceMetadataAsync(instance);

            instance.Status = InstanceStatus.Installed;
            progress?.Report("安装完成！");

            return instance;
        }
        catch (Exception ex)
        {
            instance.Status = InstanceStatus.Error;
            progress?.Report($"安装失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Launches a workflow instance
    /// </summary>
    public async Task LaunchAsync(WorkflowInstance instance, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (instance.Status != InstanceStatus.Installed && instance.Status != InstanceStatus.Stopped)
        {
            throw new InvalidOperationException($"Cannot launch workflow in status: {instance.Status}");
        }

        try
        {
            instance.Status = InstanceStatus.Starting;
            progress?.Report("启动中...");

            // Find an available port
            var port = instance.LocalPort ?? FindAvailablePort();
            instance.LocalPort = port;

            // Get the environment name
            var envName = $"ai-ready-{instance.TemplateId}";

            // Launch the process
            var launchCommand = GetLaunchCommand(instance);
            if (string.IsNullOrEmpty(launchCommand))
            {
                throw new InvalidOperationException("No launch command specified");
            }

            // Replace port placeholder in launch command
            launchCommand = launchCommand.Replace("{port}", port.ToString());

            // Start the process in background
            var process = await StartProcessAsync(envName, launchCommand, instance.InstallPath);
            
            instance.Status = InstanceStatus.Running;
            instance.LastStartedAt = DateTime.Now;

            progress?.Report($"服务已启动，端口: {port}");
        }
        catch (Exception ex)
        {
            instance.Status = InstanceStatus.Error;
            progress?.Report($"启动失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Stops a running workflow instance
    /// </summary>
    public async Task StopAsync(WorkflowInstance instance, IProgress<string>? progress = null)
    {
        if (instance.Status != InstanceStatus.Running)
        {
            return;
        }

        try
        {
            progress?.Report("正在停止...");
            instance.Status = InstanceStatus.Stopping;

            // TODO: Implement proper process termination
            // For now, just mark as stopped
            
            instance.Status = InstanceStatus.Stopped;
            progress?.Report("已停止");
        }
        catch (Exception ex)
        {
            instance.Status = InstanceStatus.Error;
            progress?.Report($"停止失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Lists all installed workflow instances
    /// </summary>
    public async Task<List<WorkflowInstance>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        var instances = new List<WorkflowInstance>();

        if (!Directory.Exists(_installBasePath))
        {
            return instances;
        }

        var directories = Directory.GetDirectories(_installBasePath);
        foreach (var dir in directories)
        {
            var metadataPath = Path.Combine(dir, "instance.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
                    var instance = System.Text.Json.JsonSerializer.Deserialize<WorkflowInstance>(json);
                    if (instance != null)
                    {
                        instance.InstallPath = dir;
                        instances.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading instance from {dir}: {ex.Message}");
                }
            }
        }

        return instances;
    }

    /// <summary>
    /// Uninstalls a workflow instance
    /// </summary>
    public async Task UninstallAsync(WorkflowInstance instance, IProgress<string>? progress = null)
    {
        if (instance.Status == InstanceStatus.Running)
        {
            await StopAsync(instance, progress);
        }

        try
        {
            progress?.Report("正在卸载...");

            // Remove conda environment
            var envName = $"ai-ready-{instance.TemplateId}";
            await _minicondaManager.RemoveEnvironmentAsync(envName);

            // Remove installation directory
            if (Directory.Exists(instance.InstallPath))
            {
                Directory.Delete(instance.InstallPath, true);
            }

            progress?.Report("卸载完成");
        }
        catch (Exception ex)
        {
            progress?.Report($"卸载失败: {ex.Message}");
            throw;
        }
    }

    private async Task RunInstallScriptAsync(string envName, string script, string workingDir, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith("echo"))
            {
                progress?.Report(trimmed.TrimStart('#').Trim());
                continue;
            }

            var result = await _minicondaManager.ExecuteInEnvironmentAsync(envName, trimmed, cancellationToken);
            
            if (!result.Success)
            {
                throw new InvalidOperationException($"Script failed: {result.StandardError}");
            }

            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                progress?.Report(result.StandardOutput);
            }
        }
    }

    private async Task SaveInstanceMetadataAsync(WorkflowInstance instance)
    {
        var metadataPath = Path.Combine(instance.InstallPath, "instance.json");
        var json = System.Text.Json.JsonSerializer.Serialize(instance, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(metadataPath, json);
    }

    private string? GetLaunchCommand(WorkflowInstance instance)
    {
        // This would typically be retrieved from the template
        // For now, return a placeholder
        return "python -m http.server {port}"; // Placeholder
    }

    private async Task<Process> StartProcessAsync(string envName, string command, string workingDir)
    {
        // Get activation script path
        var activateScript = await _minicondaManager.GetActivationScriptPathAsync(envName);
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C \"{activateScript}\" && {command}",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        return process;
    }

    private int FindAvailablePort(int startPort = 10000)
    {
        var random = new Random();
        return random.Next(startPort, 65535);
    }
}
