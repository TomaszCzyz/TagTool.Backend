using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Coravel.Scheduling.Schedule.Cron;
using Coravel.Scheduling.Schedule.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Invocables.Common;
using TagTool.BackendNew.Models;

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
    TriggerType TriggerType,
    Type InvocableType);

public class InvocablesManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventTriggeredInvocablesStorage _eventTriggeredInvocablesStorage;
    private readonly ICronTriggeredInvocablesStorage _cronTriggeredInvocablesStorage;
    private readonly IScheduler _scheduler;
    private readonly ITagToolDbContext _dbContext;

    private readonly InvocableDefinition[] _invocableDefinitions;

    public InvocablesManager(
        IServiceProvider serviceProvider,
        IEventTriggeredInvocablesStorage eventTriggeredInvocablesStorage,
        ICronTriggeredInvocablesStorage cronTriggeredInvocablesStorage,
        IScheduler scheduler,
        ITagToolDbContext dbContext)
    {
        _serviceProvider = serviceProvider;
        _eventTriggeredInvocablesStorage = eventTriggeredInvocablesStorage;
        _cronTriggeredInvocablesStorage = cronTriggeredInvocablesStorage;
        _scheduler = scheduler;
        _dbContext = dbContext;

        _invocableDefinitions = GenerateInvocableDefinitions();
    }

    private static bool ImplementsOpenGenericInterface(Type type, Type openGenericInterface)
        => type
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

    // TODO: cache this (this service is scoped)
    private static InvocableDefinition[] GenerateInvocableDefinitions()
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

        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            RespectNullableAnnotations = true,
        };
        JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true, TransformSchemaNode = TransformSchemaNode
        };

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
                triggerType,
                type);

            invocableDefinitions.Add(invocableDescriptorDto);
        }

        return invocableDefinitions.ToArray();
    }

    public InvocableDefinition[] GetInvocableDefinitions() => _invocableDefinitions;

    /// <summary>
    /// Add invocable to storage and active it based on trigger type
    /// </summary>
    /// <param name="invocableDescriptor"></param>
    /// <param name="cancellationToken"></param>
    public async Task AddAndActivateJob(InvocableDescriptor invocableDescriptor, CancellationToken cancellationToken)
    {
        var invocableDefinition = _invocableDefinitions.Single(d => d.Id == invocableDescriptor.InvocableId);

        switch (invocableDescriptor.Trigger)
        {
            case ItemTaggedTrigger trigger:
            {
                var info = ValidateEventTriggeredInvocable(trigger, invocableDefinition.InvocableType, invocableDescriptor.Args);
                // the invocable is fetched from db when event occurs, so no need for extra "run" setup
                await _eventTriggeredInvocablesStorage.Add(info);
                break;
            }
            case CronTrigger trigger:
            {
                var info = ValidateCronTriggeredInvocable(trigger, invocableDefinition.InvocableType, invocableDescriptor.Args);
                await _cronTriggeredInvocablesStorage.Add(info, cancellationToken);
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

    private EventTriggeredInvocableInfo ValidateEventTriggeredInvocable(
        ItemTaggedTrigger _,
        Type invocableType,
        string jsonPayload)
    {
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

        if (JsonSerializer.Deserialize(jsonPayload, payloadType) is not PayloadWithChangedItems payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        ValidatePayload(payload, payloadType);

        return new EventTriggeredInvocableInfo
        {
            InvocableType = invocableType,
            PayloadType = payloadType,
            Args = payload
        };
    }

    private CronTriggeredInvocableInfo ValidateCronTriggeredInvocable(
        CronTrigger trigger,
        Type invocableType,
        string jsonPayload)
    {
        // throws MalformedCronExpressionException for incorrect value
        _ = new CronExpression(trigger.CronExpression);

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

        if (JsonSerializer.Deserialize(jsonPayload, payloadType) is not PayloadWithQuery payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        ValidatePayload(payload, payloadType);

        var queryTagIds = trigger.Query.Select(param => param.TagId).ToArray();
        var tagBases = _dbContext.Tags.Where(tag => queryTagIds.Contains(tag.Id)).ToList();

        if (tagBases.Count != queryTagIds.Length)
        {
            throw new ArgumentException("Some tags in query do not exist.");
        }

        return new CronTriggeredInvocableInfo
        {
            InvocableType = invocableType,
            CronExpression = trigger.CronExpression,
            TagQuery = trigger.Query
                .Select(param => new TagQueryPart
                {
                    State = trigger.Query.First(queryParam => queryParam.TagId == param.TagId).State,
                    Tag = tagBases.First(tag => tag.Id == param.TagId)
                })
                .ToList(),
            Payload = payload,
        };
    }

    private void ValidatePayload(object payload, Type payloadType)
    {
        var service = _serviceProvider.GetService(typeof(IValidator<>).MakeGenericType(payloadType));

        if (service is not IValidator validator || !validator.CanValidateInstancesOfType(payload.GetType()))
        {
            return;
        }

        var validateMethod = typeof(IValidator<>)
            .MakeGenericType(payloadType)
            .GetMethod(nameof(IValidator.Validate), [payloadType]);

        Debug.Assert(validateMethod != null, nameof(validateMethod) + " != null");

        var result = (ValidationResult)validateMethod.Invoke(validator, [payload])!;
        if (!result.IsValid)
        {
            throw new ArgumentException($"Validation of Payload failed, errors: {string.Join("\n", result.Errors)}.");
        }
    }

    private static JsonNode TransformSchemaNode(JsonSchemaExporterContext context, JsonNode node)
    {
        // We are at Path properties.Payload.properties.<PayloadProperties>
        // This is needed, because we want to describe only "root" properties.
        if (context.Path.Length != 4)
        {
            return node;
        }

        var attributeProvider = context.PropertyInfo is not null
            ? context.PropertyInfo.AttributeProvider
            : context.TypeInfo.Type;

        var specialTypeAttr = attributeProvider?
            .GetCustomAttributes(inherit: true)
            .Select(attr => attr as SpecialTypeAttribute)
            .FirstOrDefault(attr => attr is not null);


        if (specialTypeAttr == null)
        {
            return node;
        }

        if (node is not JsonObject jObj)
        {
            throw new NotImplementedException("Handle the case where the node is a boolean");
        }

        jObj.Remove("properties");

        switch (specialTypeAttr.Type)
        {
            case SpecialTypeAttribute.Kind.DirectoryPath:
                jObj.SetAt(jObj.IndexOf("type"), "directoryPath");
                break;
            case SpecialTypeAttribute.Kind.SingleTag:
                jObj.SetAt(jObj.IndexOf("type"), "tag");
                break;
            default:
                throw new NotSupportedException($"SpecialType {specialTypeAttr.Type} is not supported");
        }

        return node;
    }
}
