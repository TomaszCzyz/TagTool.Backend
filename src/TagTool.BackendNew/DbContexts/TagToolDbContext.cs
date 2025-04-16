using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.DbContexts.Interceptors;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

public sealed class TagToolDbContext : DbContext, ITagToolDbContext, ITagToolDbContextProxy
{
    private static readonly UpdateTimestampedInterceptor _interceptor = new();

    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<TaggableItem> TaggableItems => Set<TaggableItem>();

    public DbSet<InvocableInfoBase> InvocableInfos => Set<InvocableInfoBase>();

    public DbSet<CronTriggeredInvocableInfo> CronTriggeredInvocableInfos => Set<CronTriggeredInvocableInfo>();

    public DbSet<EventTriggeredInvocableInfo> EventTriggeredInvocableInfos => Set<EventTriggeredInvocableInfo>();

    public DbSet<BackgroundInvocableInfo> BackgroundInvocableInfos => Set<BackgroundInvocableInfo>();

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.AddInterceptors(_interceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TagToolDbContext).Assembly);

        foreach (var assembly in PluginsHelper.LoadedAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        modelBuilder.Ignore<Type>();

        base.OnModelCreating(modelBuilder);
    }
}
