#nullable enable

using System.Diagnostics;
using AIReady.Shared.Contracts;
using AIReady.Shared.Models;

namespace AIReady.Local.Core.Miniconda;

/// <summary>
/// Manages Miniconda installation and Python environments
/// </summary>
public class MinicondaManager : IMinicondaService
{
    private const string DefaultInstallPath = @"C:\ProgramData\miniconda3";
    private const string CondaExeName = "conda.exe";
    
    private string? _cachedInstallPath;

    /// <inheritdoc />
    public async Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default)
    {
        var path = await GetInstallPathAsync(cancellationToken);
        return !string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, CondaExeName));
    }

    /// <inheritdoc />
    public async Task<string?> GetInstallPathAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        
        if (!string.IsNullOrEmpty(_cachedInstallPath))
        {
            return _cachedInstallPath;
        }

        // Check default location
        if (Directory.Exists(DefaultInstallPath) && File.Exists(Path.Combine(DefaultInstallPath, CondaExeName)))
        {
            _cachedInstallPath = DefaultInstallPath;
            return DefaultInstallPath;
        }

        // Check PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(';');
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                
                var condaPath = Path.Combine(path.Trim(), CondaExeName);
                if (File.Exists(condaPath))
                {
                    // Get parent directory (should be the miniconda3 folder)
                    var dir = Path.GetDirectoryName(condaPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        _cachedInstallPath = dir;
                        return dir;
                    }
                }
            }
        }

        // Check common install locations
        var commonPaths = new[]
        {
            @"C:\Users\" + Environment.UserName + @"\miniconda3",
            @"C:\Users\" + Environment.UserName + @"\anaconda3",
            @"C:\ProgramData\anaconda3",
            @"C:\miniconda3"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, CondaExeName)))
            {
                _cachedInstallPath = path;
                return path;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<InstallResult> InstallAsync(string installPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var result = new InstallResult();
        
        try
        {
            // Determine installer URL based on architecture
            var is64Bit = Environment.Is64BitOperatingSystem;
            var installerUrl = is64Bit
                ? "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe"
                : "https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86.exe";

            var tempPath = Path.Combine(Path.GetTempPath(), "miniconda_installer.exe");
            
            // Download installer
            progress?.Report(0.1);
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                
                await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var stream = await response.Content.ReadAsStreamAsync();
                
                var buffer = new byte[8192];
                long downloadedBytes = 0;
                int read;
                
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    downloadedBytes += read;
                    
                    if (totalBytes > 0)
                    {
                        var downloadProgress = 0.1 + (downloadedBytes / (double)totalBytes) * 0.3;
                        progress?.Report(downloadProgress);
                    }
                }
            }

            // Run installer silently
            progress?.Report(0.4);
            var psi = new ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = $"/S /D={installPath}",
                UseShellExecute = true,
                Verb = "runas", // Run as administrator
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                result.Success = false;
                result.Error = "无法启动安装程序";
                return result;
            }

            await Task.Run(() => process.WaitForExit(), cancellationToken);
            progress?.Report(0.9);

            if (process.ExitCode == 0)
            {
                _cachedInstallPath = installPath;
                result.Success = true;
                result.InstallPath = installPath;
                result.Message = "Miniconda 安装成功";
                
                // Initialize conda
                await RunCondaAsync("init", cancellationToken);
                
                progress?.Report(1.0);
            }
            else
            {
                result.Success = false;
                result.Error = $"安装程序返回错误码: {process.ExitCode}";
            }

            // Clean up
            try { File.Delete(tempPath); } catch { }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = "安装已取消";
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"安装失败: {ex.Message}";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<EnvironmentInfo>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var environments = new List<EnvironmentInfo>();
        
        try
        {
            var result = await RunCondaAsync("env list", cancellationToken);
            if (!result.Success) return environments;

            // Parse output like:
            // # conda environments:
            // #
            // base                  *  C:\ProgramData\miniconda3
            // myenv                    C:\ProgramData\miniconda3\envs\myenv
            
            var lines = result.StandardOutput?.Split('\n') ?? Array.Empty<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var name = parts[0];
                    var isActive = parts[1] == "*";
                    var path = isActive && parts.Length >= 3 ? parts[2] : parts[1];

                    environments.Add(new EnvironmentInfo
                    {
                        Name = name,
                        Path = path,
                        IsActive = isActive
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing environments: {ex.Message}");
        }

        return environments;
    }

    /// <inheritdoc />
    public async Task<EnvironmentInfo> CreateEnvironmentAsync(string name, string pythonVersion, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report($"正在创建环境 '{name}' (Python {pythonVersion})...");
        
        var result = await RunCondaAsync($"create -n {name} python={pythonVersion} -y", cancellationToken, progress);
        
        if (!result.Success)
        {
            throw new InvalidOperationException($"创建环境失败: {result.StandardError}");
        }

        progress?.Report("环境创建成功");
        
        return new EnvironmentInfo
        {
            Name = name,
            Path = Path.Combine(await GetInstallPathAsync(cancellationToken) ?? "", "envs", name),
            PythonVersion = pythonVersion,
            IsActive = false
        };
    }

    /// <inheritdoc />
    public async Task<bool> RemoveEnvironmentAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await RunCondaAsync($"env remove -n {name} -y", cancellationToken);
        return result.Success;
    }

    /// <inheritdoc />
    public async Task<string?> GetActivationScriptPathAsync(string envName, CancellationToken cancellationToken = default)
    {
        var installPath = await GetInstallPathAsync(cancellationToken);
        if (string.IsNullOrEmpty(installPath)) return null;

        var scriptPath = Path.Combine(installPath, "Scripts", "activate.bat");
        if (File.Exists(scriptPath))
        {
            return scriptPath;
        }

        // Alternative path for newer conda versions
        scriptPath = Path.Combine(installPath, "condabin", "activate.bat");
        if (File.Exists(scriptPath))
        {
            return scriptPath;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> InstallPackagesAsync(string envName, IEnumerable<string> packages, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var packageList = string.Join(" ", packages);
        progress?.Report($"正在安装: {packageList}");
        
        var result = await RunCondaAsync($"run -n {envName} pip install {packageList}", cancellationToken, progress);
        
        if (result.Success)
        {
            progress?.Report("安装完成");
        }
        else
        {
            progress?.Report($"安装失败: {result.StandardError}");
        }
        
        return result.Success;
    }

    /// <inheritdoc />
    public async Task<ProcessResult> ExecuteInEnvironmentAsync(string envName, string command, CancellationToken cancellationToken = default)
    {
        return await RunCondaAsync($"run -n {envName} {command}", cancellationToken);
    }

    /// <summary>
    /// Runs a conda command and returns the result
    /// </summary>
    private async Task<ProcessResult> RunCondaAsync(string arguments, CancellationToken cancellationToken, IProgress<string>? outputProgress = null)
    {
        var installPath = await GetInstallPathAsync(cancellationToken);
        if (string.IsNullOrEmpty(installPath))
        {
            return new ProcessResult { ExitCode = -1, StandardError = "Miniconda not found" };
        }

        var condaExe = Path.Combine(installPath, CondaExeName);
        
        var psi = new ProcessStartInfo
        {
            FileName = condaExe,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = installPath
        };

        using var process = new Process { StartInfo = psi };
        var output = new List<string>();
        var error = new List<string>();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.Add(e.Data);
                outputProgress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.Add(e.Data);
                outputProgress?.Report($"错误: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit(), cancellationToken);

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = string.Join("\n", output),
            StandardError = string.Join("\n", error)
        };
    }
}
