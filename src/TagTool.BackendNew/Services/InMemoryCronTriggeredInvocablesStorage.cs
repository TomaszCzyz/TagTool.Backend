using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Services;

public class InMemoryCronTriggeredInvocablesStorage : ICronTriggeredInvocablesStorage
{
    private readonly List<CronTriggeredInvocableInfo> _db = [];

    public Task<bool> Exists(CronTriggeredInvocableInfo info) => throw new NotImplementedException();

    public Task Add(CronTriggeredInvocableInfo info)
    {
        _db.Add(info);
        return Task.CompletedTask;
    }
}
