#nullable enable

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using AIReady.Shared.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AIReady.Local.Core.Workflows;

/// <summary>
/// Manages workflow templates and provides recommendations
/// </summary>
public class WorkflowRegistry
{
    private List<WorkflowTemplate> _templates = new();
    private readonly string _workflowsDirectory;

    public WorkflowRegistry()
    {
        _workflowsDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "Resources", "Workflows");
    }

    /// <summary>
    /// Loads all workflow templates from embedded resources
    /// </summary>
    public async Task LoadTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _templates.Clear();
        
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.EndsWith(".yaml") || r.EndsWith(".yml"))
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    await using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null) continue;

                    using var reader = new StreamReader(stream);
                    var yaml = await reader.ReadToEndAsync();
                    
                    var template = ParseYamlTemplate(yaml);
                    if (template != null)
                    {
                        _templates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading template {resourceName}: {ex.Message}");
                }
            }

            // Also try loading from file system (for development)
            if (Directory.Exists(_workflowsDirectory))
            {
                var yamlFiles = Directory.GetFiles(_workflowsDirectory, "*.yaml")
                    .Concat(Directory.GetFiles(_workflowsDirectory, "*.yml"));

                foreach (var file in yamlFiles)
                {
                    try
                    {
                        var yaml = await File.ReadAllTextAsync(file, cancellationToken);
                        var template = ParseYamlTemplate(yaml);
                        if (template != null && !_templates.Any(t => t.Id == template.Id))
                        {
                            _templates.Add(template);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading template from file {file}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all loaded templates
    /// </summary>
    public IReadOnlyList<WorkflowTemplate> GetAllTemplates()
    {
        return _templates.AsReadOnly();
    }

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    public WorkflowTemplate? GetTemplate(string id)
    {
        return _templates.FirstOrDefault(t => t.Id == id);
    }

    /// <summary>
    /// Gets templates filtered by category
    /// </summary>
    public IEnumerable<WorkflowTemplate> GetTemplatesByCategory(string category)
    {
        return _templates.Where(t => t.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Gets templates compatible with the given hardware
    /// </summary>
    public IEnumerable<WorkflowTemplate> GetCompatibleTemplates(HardwareInfo hardware)
    {
        var primaryGpu = hardware.GPUs.FirstOrDefault();
        var vramGb = primaryGpu?.VramBytes / (1024.0 * 1024 * 1024) ?? 0;
        var ramGb = hardware.TotalMemoryBytes / (1024.0 * 1024 * 1024);

        return _templates.Where(t =>
        {
            if (t.Requirements == null) return true;
            
            var meetsVram = vramGb >= t.Requirements.MinVramGB;
            var meetsRam = ramGb >= 8; // Minimum 8GB RAM
            
            return meetsVram && meetsRam;
        });
    }

    /// <summary>
    /// Gets recommended templates based on hardware, sorted by match quality
    /// </summary>
    public IEnumerable<WorkflowTemplate> GetRecommendedTemplates(HardwareInfo hardware)
    {
        var primaryGpu = hardware.GPUs.FirstOrDefault();
        var vramGb = primaryGpu?.VramBytes / (1024.0 * 1024 * 1024) ?? 0;

        return _templates
            .Select(t => new
            {
                Template = t,
                Score = CalculateRecommendationScore(t, vramGb)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Template);
    }

    private double CalculateRecommendationScore(WorkflowTemplate template, double vramGb)
    {
        if (template.Requirements == null) return 0.5; // Default score if no requirements

        var req = template.Requirements;
        
        // Not compatible if below minimum
        if (vramGb < req.MinVramGB) return 0;

        // Perfect score if meets optimal
        if (vramGb >= req.RecommendedVramGB) return 1.0;

        // Linear score between minimum and recommended
        var range = req.RecommendedVramGB - req.MinVramGB;
        if (range <= 0) return vramGb >= req.MinVramGB ? 1.0 : 0;

        return 0.3 + 0.7 * ((vramGb - req.MinVramGB) / range);
    }

    private WorkflowTemplate? ParseYamlTemplate(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            // First parse to a dynamic object to handle the structure
            var yamlObj = deserializer.Deserialize<Dictionary<string, object>>(yaml);
            
            // Map to our model
            var template = new WorkflowTemplate
            {
                Id = yamlObj.GetValueOrDefault("id")?.ToString() ?? "",
                Name = yamlObj.GetValueOrDefault("name")?.ToString(),
                Description = yamlObj.GetValueOrDefault("description")?.ToString(),
                Category = yamlObj.GetValueOrDefault("category")?.ToString(),
                IconUrl = yamlObj.GetValueOrDefault("icon")?.ToString(),
                Tags = yamlObj.GetValueOrDefault("tags") is List<object> tags 
                    ? tags.Select(t => t.ToString()!).ToList() 
                    : null
            };

            // Parse requirements
            if (yamlObj.GetValueOrDefault("requirements") is Dictionary<object, object> reqDict)
            {
                template.Requirements = new WorkflowRequirements
                {
                    MinVramGB = Convert.ToInt32(reqDict.GetValueOrDefault("minVramGB") ?? 0),
                    RecommendedVramGB = Convert.ToInt32(reqDict.GetValueOrDefault("recommendedVramGB") ?? 0),
                    MinDiskSpaceBytes = Convert.ToUInt64(reqDict.GetValueOrDefault("minDiskSpaceBytes") ?? 0L)
                };
            }

            // Parse local config
            if (yamlObj.GetValueOrDefault("local") is Dictionary<object, object> localDict)
            {
                template.Local = new LocalConfig
                {
                    Type = localDict.GetValueOrDefault("type")?.ToString(),
                    PythonVersion = localDict.GetValueOrDefault("pythonVersion")?.ToString(),
                    LaunchCommand = localDict.GetValueOrDefault("launchCommand")?.ToString(),
                    WebUiPort = localDict.GetValueOrDefault("webUiPort") is int port ? port : null,
                    InstallScript = localDict.GetValueOrDefault("installScript")?.ToString()
                };

                if (localDict.GetValueOrDefault("packages") is List<object> packages)
                {
                    template.Local.Packages = packages.Select(p => p.ToString()!).ToList();
                }
            }

            // Parse cloud config
            if (yamlObj.GetValueOrDefault("cloud") is Dictionary<object, object> cloudDict)
            {
                template.Cloud = new CloudConfig
                {
                    Type = cloudDict.GetValueOrDefault("type")?.ToString(),
                    ComposeFile = cloudDict.GetValueOrDefault("composeFile")?.ToString()
                };
            }

            return template;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing YAML template: {ex.Message}");
            return null;
        }
    }
}
