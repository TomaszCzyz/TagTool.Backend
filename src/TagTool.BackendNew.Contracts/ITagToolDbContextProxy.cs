using Microsoft.EntityFrameworkCore;

namespace TagTool.BackendNew.Contracts;

public interface ITagToolDbContextProxy
{
    DbSet<TagBase> Tags { get; }
    DbSet<TaggableItem> TaggableItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
#pragma warning disable CA1716 // Method name has to match name from class DbContext
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
#pragma warning restore CA1716
    int SaveChanges();
}
