using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

public sealed class TagToolDbContext : DbContext, ITagToolDbContext
{
    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<TaggableItem> TaggableItems => Set<TaggableItem>();

    public DbSet<CronTriggeredInvocableInfo> CronTriggeredInvocableInfos => Set<CronTriggeredInvocableInfo>();

    public DbSet<EventTriggeredInvocableInfo> EventTriggeredInvocableInfos => Set<EventTriggeredInvocableInfo>();

    public DbSet<BackgroundInvocableInfo> BackgroundInvocableInfos => Set<BackgroundInvocableInfo>();

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TagToolDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<Type>();
    }
}
