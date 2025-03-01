using TagTool.BackendNew.Services;

namespace TagTool.BackendNew.Contracts;

public interface IEventTriggeredInvocablesStorage
{
    Task<bool> Exists(InvocableDescriptor invocableDescriptor);
    Task Add(InvocableDescriptor invocableDescriptor);
    Task<IEnumerable<EventTriggeredInvocableInfo>> GetPayloads(ITrigger trigger);
}
