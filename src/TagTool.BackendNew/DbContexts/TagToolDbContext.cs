using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

public interface ITagToolDbContext : IDisposable, IAsyncDisposable
{
    DbSet<TagBase> Tags { get; }
    DbSet<TaggableItem> TaggedItems { get; }

    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    int SaveChanges();
}

[UsedImplicitly]
public class TagToolDbContextFactory : IDesignTimeDbContextFactory<TagToolDbContext>
{
    public TagToolDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TagToolDbContext>();
        return new TagToolDbContext(null!, optionsBuilder.Options);
    }
}

public sealed class TagToolDbContext : DbContext, ITagToolDbContext
{
    private readonly IMediator _mediator;

    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<TaggableItem> TaggedItems => Set<TaggableItem>();

    public TagToolDbContext(IMediator mediator, DbContextOptions<TagToolDbContext> options) : base(options)
    {
        _mediator = mediator;
        ChangeTracker.StateChanged += PublishNotifications;
        ChangeTracker.Tracked += PublishNotifications;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<TagBase>()
            .UseTphMappingStrategy();

        modelBuilder.Entity<TagBase>()
            .HasMany(e => e.TaggedItems)
            .WithMany(e => e.Tags)
            .UsingEntity<TagBaseTaggableItem>();

        modelBuilder
            .Entity<TagBase>()
            .HasIndex(tag => tag.Text)
            .IsUnique();

        modelBuilder
            .Entity<TagBase>()
            .Property(tag => tag.Text)
            .HasMaxLength(60)
            .UseCollation("NOCASE");

        modelBuilder
            .Entity<TaggableItem>()
            .UseTpcMappingStrategy()
            .HasKey(e => e.Id);
    }

    private void PublishNotifications(object? sender, EntityEntryEventArgs e)
    {
        switch (e.Entry.Entity)
        {
            case TagBase tagBase:
                // PublishTagCreatedOrRemoveNotification(e, tagBase);
                break;
            case TagBaseTaggableItem tagBaseTaggableItem:
                // PublishItemTaggedOrUntaggedNotification(e, tagBaseTaggableItem);
                break;
        }
    }
}
