using System.Text.Json;
using System.Text.Json.Schema;
using Coravel.Scheduling.Schedule.Cron;
using Coravel.Scheduling.Schedule.Interfaces;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Services;

public enum TriggerType
{
    Event,
    Cron,
}

public record InvocableDefinition(
    string Id,
    string GroupId,
    string DisplayName,
    string Description,
    string PayloadSchema,
    TriggerType TriggerType);

public class InvocablesManager
{
    private readonly IEventTriggeredInvocablesStorage _eventTriggeredInvocablesStorage;
    private readonly ICronTriggeredInvocablesStorage _cronTriggeredInvocablesStorage;
    private readonly IScheduler _scheduler;

    private readonly InvocableDefinition[] _invocableDefinitions;

    public InvocablesManager(
        IEventTriggeredInvocablesStorage eventTriggeredInvocablesStorage,
        ICronTriggeredInvocablesStorage cronTriggeredInvocablesStorage,
        IScheduler scheduler)
    {
        _eventTriggeredInvocablesStorage = eventTriggeredInvocablesStorage;
        _cronTriggeredInvocablesStorage = cronTriggeredInvocablesStorage;
        _scheduler = scheduler;

        _invocableDefinitions = GetEventTriggeredInvocables();
    }

    private static bool ImplementsOpenGenericInterface(Type type, Type openGenericInterface)
        => type
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

    private static InvocableDefinition[] GetEventTriggeredInvocables()
    {
        var invocableDescriptions = typeof(Program).Assembly.ExportedTypes
            .Where(x => typeof(IInvocableDescriptionBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Select(type => (
                Type: type
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInvocableDescription<>))
                    .GetGenericArguments()
                    .First(),
                Instance: (IInvocableDescriptionBase)Activator.CreateInstance(type)!))
            .ToDictionary(tuple => tuple.Type, tuple => tuple.Instance);

        var invocables = typeof(Program).Assembly.ExportedTypes
            .Where(t => IsInvocable(t) && t is { IsInterface: false, IsAbstract: false })
            .ToList();

        var options = new JsonSerializerOptions(JsonSerializerOptions.Default) { RespectNullableAnnotations = true };
        JsonSchemaExporterOptions exporterOptions = new() { TreatNullObliviousAsNonNullable = true, };

        List<InvocableDefinition> invocableDefinitions = [];
        foreach (var type in invocables)
        {
            var schema = options.GetJsonSchemaAsNode(type, exporterOptions);

            if (!invocableDescriptions.TryGetValue(type, out var description))
            {
                throw new InvalidOperationException($"No {typeof(IInvocableDescription<>).Name} implemented for Invocable {type.Name}.");
            }

            var triggerType = ImplementsOpenGenericInterface(type, typeof(IEventTriggeredInvocable<>)) ? TriggerType.Event : TriggerType.Cron;

            var invocableDescriptorDto = new InvocableDefinition(
                description.Id,
                description.GroupId,
                description.DisplayName,
                description.Description,
                schema.ToJsonString(),
                triggerType);

            invocableDefinitions.Add(invocableDescriptorDto);
        }

        return invocableDefinitions.ToArray();
    }

    public InvocableDefinition[] GetInvocableDefinitions() => _invocableDefinitions;

    /// <summary>
    /// Add invocable to storage and active it based on trigger type
    /// </summary>
    /// <param name="invocableDescriptor"></param>
    public async Task AddAndActivateJob(InvocableDescriptor invocableDescriptor)
    {
        switch (invocableDescriptor.Trigger)
        {
            case ItemTaggedTrigger:
            {
                var info = ValidateEventTriggeredInvocable(invocableDescriptor);
                // the invocable is fetched from db when event occurs, so no need for extra "run" setup
                await _eventTriggeredInvocablesStorage.Add(info);
                break;
            }
            case CronTrigger trigger:
            {
                var info = ValidateCronTriggeredInvocable(invocableDescriptor, trigger.CronExpression);
                // the invocable is fetched from db when event occurs, so no need for extra "run" setup
                await _cronTriggeredInvocablesStorage.Add(info);
                _scheduler
                    .ScheduleInvocableType(info.InvocableType)
                    .Cron(trigger.CronExpression)
                    .PreventOverlapping(info.InvocableType.FullName);

                break;
            }
        }
    }

    private static bool IsInvocable(Type t)
        => ImplementsOpenGenericInterface(t, typeof(IEventTriggeredInvocable<>))
           || ImplementsOpenGenericInterface(t, typeof(ICronTriggeredInvocable<>));

    private EventTriggeredInvocableInfo ValidateEventTriggeredInvocable(InvocableDescriptor invocableDescriptor)
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

        return new EventTriggeredInvocableInfo
        {
            InvocableType = invocableType,
            PayloadType = payloadType,
            Args = payload
        };
    }

    private CronTriggeredInvocableInfo ValidateCronTriggeredInvocable(InvocableDescriptor invocableDescriptor, string cronExpression)
    {
        // throws MalformedCronExpressionException for incorrect value
        _ = new CronExpression(cronExpression);

        var invocableType = invocableDescriptor.InvocableType;

        var @interface = invocableType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICronTriggeredInvocable<>));

        if (@interface is null)
        {
            throw new ArgumentException($"Event triggered by event must implement {typeof(ICronTriggeredInvocable<>).Name}");
        }

        var payloadType = @interface.GetGenericArguments()[0];

        if (!payloadType.IsAssignableTo(typeof(PayloadWithQuery)))
        {
            throw new ArgumentException($"Payload of event triggered by event must be {nameof(PayloadWithQuery)}");
        }

        if (JsonSerializer.Deserialize(invocableDescriptor.Args, payloadType) is not PayloadWithQuery payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        return new CronTriggeredInvocableInfo
        {
            InvocableType = invocableType,
            PayloadType = payloadType,
            CronExpression = cronExpression,
            Args = payload
        };
    }
}
