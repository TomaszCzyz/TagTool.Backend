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

        var @interface = invocableType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventTriggeredInvocable<>));

        if (@interface is null)
        {
            throw new ArgumentException($"Event triggered by event must implement {typeof(IEventTriggeredInvocable<>).Name}");
        }

        var payloadType = @interface.GetGenericArguments()[0];

        if (!payloadType.IsAssignableTo(typeof(PayloadWithChangedItems)))
        {
            throw new ArgumentException($"Payload of event triggered by event must be {nameof(PayloadWithChangedItems)}");
        }

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
