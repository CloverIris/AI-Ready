namespace AIReady.Shared.Models;

/// <summary>
/// Hardware information model
/// </summary>
public class HardwareInfo
{
    public string? CpuModel { get; set; }
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public ulong TotalMemoryBytes { get; set; }
    public ulong AvailableMemoryBytes { get; set; }
    public List<GPUInfo> GPUs { get; set; } = new();
    public List<DiskInfo> Disks { get; set; } = new();
    public string? OSVersion { get; set; }
    public bool IsCompatible { get; set; }
    public string? CompatibilityMessage { get; set; }
}

public class GPUInfo
{
    public string? Name { get; set; }
    public ulong VramBytes { get; set; }
    public int? CudaCores { get; set; }
    public string? DriverVersion { get; set; }
    public bool IsNvidia => Name?.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ?? false;
    public bool IsAmd => Name?.Contains("AMD", StringComparison.OrdinalIgnoreCase) ?? false;
    public bool IsIntel => Name?.Contains("Intel", StringComparison.OrdinalIgnoreCase) ?? false;
}

public class DiskInfo
{
    public string? DriveLetter { get; set; }
    public ulong TotalBytes { get; set; }
    public ulong AvailableBytes { get; set; }
    public string? FileSystem { get; set; }
}

/// <summary>
/// Compatibility assessment result
/// </summary>
public class CompatibilityResult
{
    public bool IsCompatible { get; set; }
    public CompatibilityLevel Level { get; set; }
    public List<CompatibilityIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public enum CompatibilityLevel
{
    NotCompatible,
    Minimum,
    Recommended,
    Optimal
}

public class CompatibilityIssue
{
    public string? Component { get; set; }
    public string? Description { get; set; }
    public IssueSeverity Severity { get; set; }
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error
}
