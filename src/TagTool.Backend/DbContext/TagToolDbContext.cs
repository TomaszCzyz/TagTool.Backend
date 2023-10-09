using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TagTool.Backend.Events;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.DbContext;

public interface ITagToolDbContext
{
    DbSet<TagBase> Tags { get; }
    DbSet<TextTag> NormalTags { get; }
    DbSet<TagSynonymsGroup> TagSynonymsGroups { get; }
    DbSet<TagsHierarchy> TagsHierarchy { get; }
    DbSet<TaggableItem> TaggedItems { get; }
    DbSet<TaggableFile> TaggableFiles { get; }
    DbSet<TaggableFolder> TaggableFolders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class TagToolDbContext : Microsoft.EntityFrameworkCore.DbContext, ITagToolDbContext
{
    private readonly IMediator _mediator;

    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<TextTag> NormalTags => Set<TextTag>();

    public DbSet<TagSynonymsGroup> TagSynonymsGroups => Set<TagSynonymsGroup>();

    public DbSet<TagsHierarchy> TagsHierarchy => Set<TagsHierarchy>();

    public DbSet<TaggableItem> TaggedItems => Set<TaggableItem>();

    public DbSet<TaggableFile> TaggableFiles => Set<TaggableFile>();

    public DbSet<TaggableFolder> TaggableFolders => Set<TaggableFolder>();

    public TagToolDbContext(IMediator mediator, DbContextOptions<TagToolDbContext> options) : base(options)
    {
        _mediator = mediator;
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
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
            .HasIndex(tag => tag.FormattedName)
            .IsUnique();

        modelBuilder
            .Entity<TagBase>()
            .Property(tag => tag.FormattedName)
            .UseCollation("NOCASE");

        modelBuilder
            .Entity<TextTag>()
            .Property(tag => tag.Text)
            .UseCollation("NOCASE");

        modelBuilder
            .Entity<DayRangeTag>()
            .Property(tag => tag.Begin)
            .HasConversion<int>();

        modelBuilder
            .Entity<DayTag>()
            .HasData(Enum.GetValues<DayOfWeek>().Select(day => new DayTag { Id = 1000 + (int)day, DayOfWeek = day }));

        modelBuilder
            .Entity<MonthTag>()
            .HasData(Enumerable.Range(1, 12).Select(month => new MonthTag { Id = 2000 + month, Month = month }));

        modelBuilder
            .Entity<ItemTypeTag>()
            .Property(tag => tag.Type)
            .HasConversion<string>(type => type.AssemblyQualifiedName!, s => Type.GetType(s)!)
            .UsePropertyAccessMode(PropertyAccessMode.Property);

        modelBuilder.Entity<ItemTypeTag>()
            .HasData(new ItemTypeTag { Id = 3002, Type = typeof(TaggableFile) }, new ItemTypeTag { Id = 3003, Type = typeof(TaggableFolder) });

        modelBuilder
            .Entity<TaggableItem>()
            .UseTpcMappingStrategy()
            .HasKey(e => e.Id);

        modelBuilder
            .Entity<TaggableFile>()
            .HasIndex(file => file.Path)
            .IsUnique();

        modelBuilder
            .Entity<TaggableFolder>()
            .HasIndex(folder => folder.Path)
            .IsUnique();
    }

    private static void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry.Entity is not IHasTimestamps entityWithTimestamps)
        {
            return;
        }

        switch (e.Entry.State)
        {
            case EntityState.Deleted:
                entityWithTimestamps.Deleted = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                entityWithTimestamps.Modified = DateTime.UtcNow;
                break;
            case EntityState.Added:
                entityWithTimestamps.Added = DateTime.UtcNow;
                break;
        }
    }

    private void PublishNotifications(object? sender, EntityEntryEventArgs e)
    {
        switch (e.Entry.Entity)
        {
            case TagBase tagBase:
                PublishTagCreatedOrRemoveNotification(e, tagBase);
                break;
            case TagBaseTaggableItem tagBaseTaggableItem:
                PublishItemTaggedOrUntaggedNotification(e, tagBaseTaggableItem);
                break;
        }
    }

    private void PublishItemTaggedOrUntaggedNotification(EntityEntryEventArgs e, TagBaseTaggableItem item)
    {
        switch (e.Entry.State)
        {
            case EntityState.Added:
                _mediator.Publish(new ItemTaggedNotif { TaggableItemId = item.TaggableItemId, AddedTagId = item.TagBaseId });
                break;
            case EntityState.Deleted:
                _mediator.Publish(new ItemUntaggedNotif { TaggableItemId = item.TaggableItemId, RemovedTagId = item.TagBaseId });
                break;
        }
    }

    private void PublishTagCreatedOrRemoveNotification(EntityEntryEventArgs e, TagBase tagBase)
    {
        switch (e.Entry.State)
        {
            case EntityState.Added:
                _mediator.Publish(new TagCreatedNotification { Tag = tagBase });
                break;
            case EntityState.Deleted:
                _mediator.Publish(new TagDeletedNotification { Tag = tagBase });
                break;
        }
    }
}
