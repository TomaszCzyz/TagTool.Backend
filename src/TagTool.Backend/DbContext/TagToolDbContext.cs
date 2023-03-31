using Microsoft.EntityFrameworkCore;
using TagTool.Backend.Models;

namespace TagTool.Backend.DbContext;

public class TagToolDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<TaggedItem> TaggedItems { get; set; } = null!;

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>()
            .HasIndex(tag => tag.Name)
            .IsUnique();
    }
}
