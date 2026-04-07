namespace AIReady.Shared.Models;

/// <summary>
/// AI workflow template
/// </summary>
public class WorkflowTemplate
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    
    public WorkflowRequirements? Requirements { get; set; }
    public LocalConfig? Local { get; set; }
    public CloudConfig? Cloud { get; set; }
    
    public List<string>? Tags { get; set; }
    public int Popularity { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WorkflowRequirements
{
    public int MinVramGB { get; set; }
    public int RecommendedVramGB { get; set; }
    public ulong MinDiskSpaceBytes { get; set; }
    public List<string>? RequiredSoftware { get; set; }
}

public class LocalConfig
{
    public string? Type { get; set; } // "conda", "docker"
    public string? PythonVersion { get; set; }
    public List<string>? Packages { get; set; }
    public string? InstallScript { get; set; }
    public string? LaunchCommand { get; set; }
    public int? WebUiPort { get; set; }
}

public class CloudConfig
{
    public string? Type { get; set; } // "docker"
    public string? ComposeFile { get; set; }
    public List<PortMapping>? PortMappings { get; set; }
    public List<VolumeMapping>? VolumeMappings { get; set; }
}

public class PortMapping
{
    public int HostPort { get; set; }
    public int ContainerPort { get; set; }
}

public class VolumeMapping
{
    public string? HostPath { get; set; }
    public string? ContainerPath { get; set; }
}

/// <summary>
/// Installed workflow instance
/// </summary>
public class WorkflowInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? TemplateId { get; set; }
    public string? Name { get; set; }
    public InstallTarget Target { get; set; }
    public InstanceStatus Status { get; set; }
    public string? InstallPath { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime? LastStartedAt { get; set; }
    public int? LocalPort { get; set; }
    public string? ConnectionId { get; set; } // For cloud instances
}

public enum InstallTarget
{
    Local,
    Cloud
}

public enum InstanceStatus
{
    Installing,
    Installed,
    Starting,
    Running,
    Stopping,
    Stopped,
    Error
}
