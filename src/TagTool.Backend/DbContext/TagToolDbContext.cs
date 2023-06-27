﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TagTool.Backend.Models;

namespace TagTool.Backend.DbContext;

public sealed class TagToolDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<NormalTag> NormalTags => Set<NormalTag>();

    public DbSet<DateRangeTag> DateRangeTags => Set<DateRangeTag>();

    public DbSet<SizeRangeTag> SizeRangeTags => Set<SizeRangeTag>();

    public DbSet<ItemTypeTag> ItemTypeTags => Set<ItemTypeTag>();

    public DbSet<YearTag> YearTags => Set<YearTag>();

    public DbSet<MonthTag> MonthTags => Set<MonthTag>();

    public DbSet<MonthRangeTag> MonthRangeTags => Set<MonthRangeTag>();

    public DbSet<DayTag> DayTags => Set<DayTag>();

    public DbSet<DayRangeTag> DayRangeTags => Set<DayRangeTag>();

    public DbSet<TaggableItem> TaggedItemsBase => Set<TaggableItem>();

    public DbSet<TaggableFile> TaggableFiles => Set<TaggableFile>();

    public DbSet<TaggableFolder> TaggableFolders => Set<TaggableFolder>();

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options) : base(options)
    {
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<TagBase>()
            .UseTphMappingStrategy();

        modelBuilder
            .Entity<TagBase>()
            .HasIndex(tag => tag.FormattedName)
            .IsUnique();

        modelBuilder
            .Entity<TagBase>()
            .Property(tag => tag.FormattedName)
            .UseCollation("NOCASE");

        modelBuilder
            .Entity<NormalTag>()
            .Property(tag => tag.Name)
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
        if (e.Entry.Entity is not IHasTimestamps entityWithTimestamps) return;

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
}
