namespace AIReady.Shared.Models;

/// <summary>
/// Python environment information
/// </summary>
public class EnvironmentInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? Name { get; set; }
    public string? PythonVersion { get; set; }
    public string? Path { get; set; }
    public List<PackageInfo> Packages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; }
    public bool IsBase { get; set; }
}

public class PackageInfo
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Channel { get; set; }
}

/// <summary>
/// System PATH environment variable entry
/// </summary>
public class PathEntry
{
    public string? Value { get; set; }
    public bool Exists { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}
