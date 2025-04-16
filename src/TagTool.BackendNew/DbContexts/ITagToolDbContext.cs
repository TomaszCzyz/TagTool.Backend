using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

public interface ITagToolDbContext : IDisposable, IAsyncDisposable
{
    DbSet<TagBase> Tags { get; }
    DbSet<TaggableItem> TaggableItems { get; }

    DbSet<InvocableInfoBase> InvocableInfos { get; }
    DbSet<CronTriggeredInvocableInfo> CronTriggeredInvocableInfos { get; }
    DbSet<EventTriggeredInvocableInfo> EventTriggeredInvocableInfos { get; }
    DbSet<BackgroundInvocableInfo> BackgroundInvocableInfos { get; }

    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
#pragma warning disable CA1716 // Method name has to match name from class DbContext
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
#pragma warning restore CA1716
    int SaveChanges();
}
