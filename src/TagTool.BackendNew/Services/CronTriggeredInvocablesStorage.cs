using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services;

public class CronTriggeredInvocablesStorage : ICronTriggeredInvocablesStorage
{
    private readonly ITagToolDbContext _dbContext;

    public CronTriggeredInvocablesStorage(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> Exists(CronTriggeredInvocableInfo info) => throw new NotImplementedException();

    public async Task Add(CronTriggeredInvocableInfo info, CancellationToken cancellationToken)
    {
        _dbContext.CronTriggeredInvocableInfos.Add(info);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
