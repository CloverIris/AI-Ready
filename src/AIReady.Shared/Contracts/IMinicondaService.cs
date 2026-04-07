namespace AIReady.Shared.Contracts;

/// <summary>
/// Service for managing Miniconda installation and environments
/// </summary>
public interface IMinicondaService
{
    /// <summary>
    /// Check if Miniconda is already installed
    /// </summary>
    Task<bool> IsInstalledAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the installation path
    /// </summary>
    Task<string?> GetInstallPathAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Install Miniconda silently
    /// </summary>
    Task<InstallResult> InstallAsync(string installPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List all environments
    /// </summary>
    Task<List<Models.EnvironmentInfo>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a new environment
    /// </summary>
    Task<Models.EnvironmentInfo> CreateEnvironmentAsync(string name, string pythonVersion, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete an environment
    /// </summary>
    Task<bool> RemoveEnvironmentAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activate an environment (returns the path to activate script)
    /// </summary>
    Task<string?> GetActivationScriptPathAsync(string envName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Install packages in an environment
    /// </summary>
    Task<bool> InstallPackagesAsync(string envName, IEnumerable<string> packages, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a command in an environment
    /// </summary>
    Task<ProcessResult> ExecuteInEnvironmentAsync(string envName, string command, CancellationToken cancellationToken = default);
}

public class InstallResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? InstallPath { get; set; }
    public string? Error { get; set; }
}

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string? StandardOutput { get; set; }
    public string? StandardError { get; set; }
    public bool Success => ExitCode == 0;
}
