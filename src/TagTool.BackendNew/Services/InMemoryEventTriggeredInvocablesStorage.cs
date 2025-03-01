using System.Text.Json;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables;
using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Services;

public record EventTriggeredInvocableInfo(Type InvocableType, Type PayloadType, PayloadWithChangedItems Args);

public class InMemoryEventTriggeredInvocablesStorage : IEventTriggeredInvocablesStorage
{
    private readonly List<EventTriggeredInvocableInfo> _db = [];

    public Task<bool> Exists(InvocableDescriptor invocableDescriptor) => throw new NotImplementedException();

    public Task Add(InvocableDescriptor invocableDescriptor)
    {
        if (invocableDescriptor.Trigger is not ItemTaggedTrigger)
        {
            throw new ArgumentException("Incorrect trigger type");
        }

        var invocableType = invocableDescriptor.InvocableType;

        if (!invocableType.IsAssignableTo(typeof(IEventTriggeredInvocable<PayloadWithChangedItems>)))
        {
            throw new ArgumentException("Incorrect invocable type");
        }

        var payloadType = invocableType.GetGenericArguments()[0];

        if (JsonSerializer.Deserialize(invocableDescriptor.Args, payloadType) is not PayloadWithChangedItems payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        var invocableInfo = new EventTriggeredInvocableInfo(invocableType, payloadType, payload);

        _db.Add(invocableInfo);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<EventTriggeredInvocableInfo>> GetPayloads(ITrigger trigger)
    {
        return Task.FromResult(_db.AsEnumerable());
    }
}
