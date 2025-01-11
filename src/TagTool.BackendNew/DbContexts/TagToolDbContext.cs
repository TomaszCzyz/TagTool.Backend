using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

// [UsedImplicitly]
// public class TagToolDbContextFactory : IDesignTimeDbContextFactory<TagToolDbContext>
// {
//     public TagToolDbContext CreateDbContext(string[] args)
//     {
//         var optionsBuilder = new DbContextOptionsBuilder<TagToolDbContext>();
//         return new TagToolDbContext(optionsBuilder.Options);
//     }
// }

public sealed class TagToolDbContext : DbContext, ITagToolDbContext
{
    public DbSet<TagBase> Tags => Set<TagBase>();

    public DbSet<TaggableItem> TaggedItems => Set<TaggableItem>();

    public TagToolDbContext(DbContextOptions<TagToolDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TagToolDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
