using System.IO;
using AIReady.Shared.Models;

namespace AIReady.Shared.Contracts;

/// <summary>
/// Service for SSH connections and operations
/// </summary>
public interface ISshService : IDisposable
{
    /// <summary>
    /// Current connection status
    /// </summary>
    ConnectionStatus Status { get; }
    
    /// <summary>
    /// Connect to server
    /// </summary>
    Task<bool> ConnectAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnect from server
    /// </summary>
    Task DisconnectAsync();
    
    /// <summary>
    /// Execute a command on the remote server
    /// </summary>
    Task<RemoteCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload a file via SFTP
    /// </summary>
    Task<bool> UploadFileAsync(string localPath, string remotePath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload data via SFTP
    /// </summary>
    Task<bool> UploadDataAsync(byte[] data, string remotePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download a file via SFTP
    /// </summary>
    Task<bool> DownloadFileAsync(string remotePath, string localPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List files in a remote directory
    /// </summary>
    Task<List<RemoteFileInfo>> ListDirectoryAsync(string remotePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Create a port forwarding tunnel
    /// </summary>
    Task<bool> CreateTunnelAsync(int localPort, string remoteHost, int remotePort, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when connection status changes
    /// </summary>
    event EventHandler<ConnectionStatus>? StatusChanged;
    
    /// <summary>
    /// Event fired when connection is lost
    /// </summary>
    event EventHandler? ConnectionLost;
}

public class RemoteCommandResult
{
    public int ExitCode { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public bool Success => ExitCode == 0;
}

public class RemoteFileInfo
{
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime LastModified { get; set; }
    public string? Permissions { get; set; }
}
