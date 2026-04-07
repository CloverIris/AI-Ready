using AIReady.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace AIReady.Service.Data;

public class AIReadyDbContext : DbContext
{
    public AIReadyDbContext(DbContextOptions<AIReadyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<CloudConfig> CloudConfigs { get; set; }
    public DbSet<ContentItem> ContentItems { get; set; }
    public DbSet<WorkflowTemplateEntity> WorkflowTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<CloudConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Configs)
                  .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.PublishedAt);
        });

        modelBuilder.Entity<WorkflowTemplateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Category);
        });
    }
}
