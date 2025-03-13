using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Services;

public class InMemoryCronTriggeredInvocablesStorage : ICronTriggeredInvocablesStorage
{
    private readonly List<CronTriggeredInvocableInfo> _db = [];

    public Task<bool> Exists(CronTriggeredInvocableInfo info) => throw new NotImplementedException();

    public Task Add(CronTriggeredInvocableInfo info, CancellationToken cancellationToken)
    {
        _db.Add(info);
        return Task.CompletedTask;
    }
}
