using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Interceptors;

public class UpdateTimestampedInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateAuditableEntities(DbContext context)
    {
        var utcNow = DateTime.UtcNow;
        var entities = context.ChangeTracker.Entries<ITimestamped>().ToList();

        foreach (var entry in entities)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(ITimestamped.CreatedOnUtc)).CurrentValue = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(ITimestamped.ModifiedOnUtc)).CurrentValue = utcNow;
                    break;
            }
        }
    }
}
