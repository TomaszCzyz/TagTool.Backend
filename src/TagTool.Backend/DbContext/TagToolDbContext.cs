using Microsoft.EntityFrameworkCore;
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

    // public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<TaggedItem> TaggedItems => Set<TaggedItem>();

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options) : base(options)
    {
        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagBase>().UseTphMappingStrategy().HasIndex(@base => @base.Id);
        modelBuilder.Entity<NormalTag>();
        modelBuilder.Entity<DateRangeTag>();
        modelBuilder.Entity<SizeRangeTag>();
        modelBuilder.Entity<ItemTypeTag>();

        modelBuilder.Entity<Tag>()
            .HasIndex(tag => tag.Name)
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .Property(tag => tag.Name)
            .UseCollation("NOCASE");
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
