namespace AIReady.Shared.Contracts;

/// <summary>
/// Service for remote Docker management
/// </summary>
public interface IDockerService
{
    /// <summary>
    /// Check if Docker is available on the remote server
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get Docker version info
    /// </summary>
    Task<DockerVersionInfo> GetVersionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List all containers
    /// </summary>
    Task<List<ContainerInfo>> ListContainersAsync(bool all = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deploy a container from a workflow
    /// </summary>
    Task<ContainerInfo> DeployAsync(DeployRequest request, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start a container
    /// </summary>
    Task<bool> StartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop a container
    /// </summary>
    Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a container
    /// </summary>
    Task<bool> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get container logs
    /// </summary>
    Task<string> GetContainerLogsAsync(string containerId, int tail = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stream container logs
    /// </summary>
    IAsyncEnumerable<string> StreamLogsAsync(string containerId, CancellationToken cancellationToken = default);
}

public class DockerVersionInfo
{
    public string? Version { get; set; }
    public string? ApiVersion { get; set; }
    public string? Platform { get; set; }
}

public class ContainerInfo
{
    public string? Id { get; set; }
    public List<string>? Names { get; set; }
    public string? Image { get; set; }
    public string? Status { get; set; }
    public string? State { get; set; }
    public List<ContainerPort>? Ports { get; set; }
    public long Created { get; set; }
    public bool GPU { get; set; }
}

public class ContainerPort
{
    public int PrivatePort { get; set; }
    public int PublicPort { get; set; }
    public string? Type { get; set; }
}

public class DeployRequest
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public Dictionary<string, string>? Ports { get; set; } // hostPort -> containerPort
    public Dictionary<string, string>? Volumes { get; set; } // hostPath -> containerPath
    public Dictionary<string, string>? Environment { get; set; }
    public bool GPU { get; set; }
}
