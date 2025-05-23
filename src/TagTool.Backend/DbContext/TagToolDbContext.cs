﻿using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TagTool.Backend.Events;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.DbContext;

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

    public DbSet<EventTaskDto> EventTasks => Set<EventTaskDto>();

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
            .HasMaxLength(60)
            .UseCollation("NOCASE");

        modelBuilder
            .Entity<TextTag>()
            .Property(tag => tag.Text)
            .HasMaxLength(32)
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
            .Entity<TaggableFile>()
            .Property(file => file.Path)
            .HasMaxLength(260);

        modelBuilder
            .Entity<TaggableFolder>()
            .HasIndex(folder => folder.Path)
            .IsUnique();

        modelBuilder
            .Entity<TaggableFolder>()
            .Property(file => file.Path)
            .HasMaxLength(260);

        modelBuilder
            .Entity<TagSynonymsGroup>()
            .Property(group => group.Name)
            .HasMaxLength(40);

        modelBuilder
            .Entity<EventTaskDto>()
            .HasKey(dto => dto.TaskId);

        modelBuilder
            .Entity<EventTaskDto>()
            .Property(dto => dto.TaskId)
            .HasMaxLength(20);

        modelBuilder
            .Entity<EventTaskDto>()
            .Property(dto => dto.ActionId)
            .HasMaxLength(20);

        modelBuilder
            .Entity<EventTaskDto>()
            .Property(dto => dto.ActionAttributes)
            .HasConversion(
                dictionary => JsonSerializer.Serialize(dictionary, JsonSerializerOptions.Default),
                s => JsonSerializer.Deserialize<Dictionary<string, string>?>(s, JsonSerializerOptions.Default) ?? new Dictionary<string, string>());

        modelBuilder
            .Entity<EventTaskDto>()
            .Property(dto => dto.Events)
            .HasConversion(
                eventNames => JsonSerializer.Serialize(eventNames, JsonSerializerOptions.Default),
                s => JsonSerializer.Deserialize<string[]>(s, JsonSerializerOptions.Default) ?? Array.Empty<string>());
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
                // PublishTagCreatedOrRemoveNotification(e, tagBase);
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
                _mediator.Publish(new ItemTaggedChanged { TaggableItemId = item.TaggableItemId, AddedTagId = item.TagBaseId });
                break;
            case EntityState.Deleted:
                _mediator.Publish(new ItemUntaggedChanged { TaggableItemId = item.TaggableItemId, RemovedTagId = item.TagBaseId });
                break;
        }
    }

    // private void PublishTagCreatedOrRemoveNotification(EntityEntryEventArgs e, TagBase tagBase)
    // {
    //     switch (e.Entry.State)
    //     {
    //         case EntityState.Added:
    //             _mediator.Publish(new TagCreatedNotification { Tag = tagBase });
    //             break;
    //         case EntityState.Deleted:
    //             _mediator.Publish(new TagDeletedNotification { Tag = tagBase });
    //             break;
    //     }
    // }
}
