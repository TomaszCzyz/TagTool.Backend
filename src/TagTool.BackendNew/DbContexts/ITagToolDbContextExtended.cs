using Microsoft.EntityFrameworkCore;
using TagTool.BackendNew.Contracts.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts;

public interface ITagToolDbContextExtended : ITagToolDbContext
{
    DbSet<CronTriggeredInvocableInfo> CronTriggeredInvocableInfos { get; }
    DbSet<EventTriggeredInvocableInfo> EventTriggeredInvocableInfos { get; }
    DbSet<BackgroundInvocableInfo> BackgroundInvocableInfos { get; }
}
