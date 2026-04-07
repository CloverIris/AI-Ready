namespace AIReady.Shared.Models;

/// <summary>
/// SSH connection configuration
/// </summary>
public class ConnectionInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? Name { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 22;
    public string? Username { get; set; }
    public AuthType AuthenticationType { get; set; }
    public string? EncryptedCredential { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastConnectedAt { get; set; }
    public ConnectionStatus Status { get; set; }
}

public enum AuthType
{
    Password,
    PrivateKey
}

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// Cloud server information
/// </summary>
public class ServerInfo
{
    public string? Hostname { get; set; }
    public string? Platform { get; set; }
    public CpuInfo? CPU { get; set; }
    public MemoryInfo? Memory { get; set; }
    public List<GPUInfo>? GPUs { get; set; }
    public DiskInfo? Disk { get; set; }
    public bool DockerAvailable { get; set; }
    public string? AgentVersion { get; set; }
}

public class CpuInfo
{
    public string? Model { get; set; }
    public int Cores { get; set; }
    public int Threads { get; set; }
    public double UsagePercent { get; set; }
}

public class MemoryInfo
{
    public ulong Total { get; set; }
    public ulong Used { get; set; }
    public ulong Free { get; set; }
    public double UsedPercent { get; set; }
}
