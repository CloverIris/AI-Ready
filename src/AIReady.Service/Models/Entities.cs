namespace AIReady.Service.Models;

/// <summary>
/// User entity
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public List<CloudConfig> Configs { get; set; } = new();
}

/// <summary>
/// User's cloud configuration sync entity
/// </summary>
public class CloudConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? ConfigData { get; set; } // JSON
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public User? User { get; set; }
}

/// <summary>
/// Content item for the learning section
/// </summary>
public class ContentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Type { get; set; } // tutorial, news, workflow
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public List<string>? Tags { get; set; }
    public int ViewCount { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Workflow template entity
/// </summary>
public class WorkflowTemplateEntity
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? RequirementsJson { get; set; }
    public string? LocalConfigJson { get; set; }
    public string? CloudConfigJson { get; set; }
    public string? TagsJson { get; set; }
    public int Popularity { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
