using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Services;

public class InMemoryEventTriggeredInvocablesStorage : IEventTriggeredInvocablesStorage
{
    private readonly List<EventTriggeredInvocableInfo> _db = [];

    public Task<bool> Exists(EventTriggeredInvocableInfo info) => throw new NotImplementedException();

    public Task Add(EventTriggeredInvocableInfo info)
    {
        _db.Add(info);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<EventTriggeredInvocableInfo>> GetPayloads()
    {
        return Task.FromResult(_db.AsEnumerable());
    }
}
