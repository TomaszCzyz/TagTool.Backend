using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.DbContext;

public interface ITagToolDbContext : IDisposable, IAsyncDisposable
{
    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
    DbSet<TagBase> Tags { get; }
    DbSet<TextTag> NormalTags { get; }
    DbSet<TagSynonymsGroup> TagSynonymsGroups { get; }
    DbSet<TagsHierarchy> TagsHierarchy { get; }
    DbSet<TaggableItem> TaggedItems { get; }
    DbSet<TaggableFile> TaggableFiles { get; }
    DbSet<TaggableFolder> TaggableFolders { get; }
    DbSet<EventTaskDto> EventTasks { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    int SaveChanges();
}
