using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Contracts.Internal;

public class EventTriggeredInvocableInfo
{
    public required Type InvocableType { get; init; }
    public required Type PayloadType { get; init; }
    public required PayloadWithChangedItems Args { get; init; }

    public void Deconstruct(out Type invocableType, out Type payloadType, out PayloadWithChangedItems args)
    {
        invocableType = InvocableType;
        payloadType = PayloadType;
        args = Args;
    }
}

public interface IEventTriggeredInvocablesStorage
{
    Task<bool> Exists(EventTriggeredInvocableInfo info);
    Task Add(EventTriggeredInvocableInfo info);
    Task<IEnumerable<EventTriggeredInvocableInfo>> GetPayloads();
}
