using System.Diagnostics;
using System.Text.Json;
using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Cron;
using Coravel.Scheduling.Schedule.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Contracts.Invocables.Common;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Mappers;
using TagTool.BackendNew.Models;
using BackgroundTrigger = TagTool.BackendNew.Models.BackgroundTrigger;
using CronTrigger = TagTool.BackendNew.Models.CronTrigger;

namespace TagTool.BackendNew.Services;

public class InvocablesManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IScheduler _scheduler;
    private readonly ITagToolDbContext _dbContext;

    private readonly InvocableDefinition[] _invocableDefinitions;

    public InvocablesManager(
        IServiceProvider serviceProvider,
        IScheduler scheduler,
        ITagToolDbContext dbContext,
        InvocableDefinition[] invocableDefinitions)
    {
        _serviceProvider = serviceProvider;
        _scheduler = scheduler;
        _dbContext = dbContext;
        _invocableDefinitions = invocableDefinitions;
    }

    public InvocableDefinition[] GetInvocableDefinitions() => _invocableDefinitions;

    public IEnumerable<InvocableDescriptor> GetInvocables(int pageSize, int pageNumber)
    {
        var t = _dbContext.InvocableInfos
            .OrderBy(t => t.ModifiedOnUtc)
            .Skip(pageNumber * pageSize)
            .Take(pageSize);

        foreach (var invocableInfo in t)
        {
            ITrigger trigger = invocableInfo switch
            {
                BackgroundInvocableInfo => new BackgroundTrigger(),
                CronTriggeredInvocableInfo cronTriggeredInvocableInfo
                    => new CronTrigger
                    {
                        CronExpression = cronTriggeredInvocableInfo.CronExpression,
                        Query = cronTriggeredInvocableInfo.TagQuery
                    },
                EventTriggeredInvocableInfo => ItemTaggedTrigger.Instance,
                _ => throw new UnreachableException()
            };

            yield return new InvocableDescriptor
            {
                InvocableId = invocableInfo.InvocableId,
                Trigger = trigger,
                Args = invocableInfo.Payload
            };
        }
    }

    /// <summary>
    /// Add invocable to storage and active it based on trigger type
    /// </summary>
    public async Task AddAndActivateInvocable(InvocableDescriptor invocableDescriptor, CancellationToken cancellationToken)
    {
        var invocableDefinition = _invocableDefinitions.Single(d => d.Id == invocableDescriptor.InvocableId);

        switch (invocableDescriptor.Trigger)
        {
            case ItemTaggedTrigger trigger:
            {
                var info = ValidateEventTriggeredInvocable(trigger, invocableDefinition, invocableDescriptor.Args);

                info.Id = Guid.CreateVersion7();
                _dbContext.EventTriggeredInvocableInfos.Add(info);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);

                // the invocable is fetched from db when event occurs, so no need for extra "run" setup
                break;
            }
            case CronTrigger trigger:
            {
                var info = ValidateCronTriggeredInvocable(trigger, invocableDefinition, invocableDescriptor.Args);

                info.Id = Guid.CreateVersion7();
                _dbContext.CronTriggeredInvocableInfos.Add(info);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);

                _scheduler
                    .ScheduleInvocableType(info.InvocableType)
                    .Cron(trigger.CronExpression)
                    .PreventOverlapping(info.InvocableType.FullName);

                break;
            }
            case BackgroundTrigger trigger:
            {
                var info = ValidateBackgroundInvocable(trigger, invocableDefinition, invocableDescriptor.Args);

                info.Id = Guid.CreateVersion7();
                _dbContext.BackgroundInvocableInfos.Add(info);
                _ = await _dbContext.SaveChangesAsync(cancellationToken);

                var service = (IInvocable)_serviceProvider.GetRequiredService(info.InvocableType);
                await service.Invoke();

                break;
            }
        }
    }

    private BackgroundInvocableInfo ValidateBackgroundInvocable(
        BackgroundTrigger trigger,
        InvocableDefinition invocableDefinition,
        string jsonPayload)
    {
        ValidateInterfaces(invocableDefinition.InvocableType, typeof(IBackgroundInvocable<>), typeof(PayloadWithQuery), out var payloadExactType);

        if (JsonSerializer.Deserialize(jsonPayload, payloadExactType) is not PayloadWithQuery payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        ValidatePayload(payload, payloadExactType);

        return new BackgroundInvocableInfo
        {
            InvocableId = invocableDefinition.Id,
            InvocableType = invocableDefinition.InvocableType,
            InvocablePayloadType = payloadExactType,
            Payload = jsonPayload,
        };
    }

    private EventTriggeredInvocableInfo ValidateEventTriggeredInvocable(
        ItemTaggedTrigger _,
        InvocableDefinition invocableDefinition,
        string jsonPayload)
    {
        ValidateInterfaces(
            invocableDefinition.InvocableType,
            typeof(IEventTriggeredInvocable<>),
            typeof(PayloadWithChangedItems),
            out var payloadExactType);

        if (JsonSerializer.Deserialize(jsonPayload, payloadExactType) is not PayloadWithChangedItems payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        ValidatePayload(payload, payloadExactType);

        return new EventTriggeredInvocableInfo
        {
            InvocableId = invocableDefinition.Id,
            Payload = jsonPayload,
            InvocableType = invocableDefinition.InvocableType,
            InvocablePayloadType = payloadExactType,
        };
    }

    private CronTriggeredInvocableInfo ValidateCronTriggeredInvocable(
        CronTrigger trigger,
        InvocableDefinition invocableDefinition,
        string jsonPayload)
    {
        // throws MalformedCronExpressionException for incorrect value
        _ = new CronExpression(trigger.CronExpression);

        ValidateInterfaces(invocableDefinition.InvocableType, typeof(ICronTriggeredInvocable<>), typeof(PayloadWithQuery), out var payloadExactType);

        if (JsonSerializer.Deserialize(jsonPayload, payloadExactType) is not PayloadWithQuery payload)
        {
            throw new ArgumentException("Incorrect Payload");
        }

        ValidatePayload(payload, payloadExactType);

        var queryTagIds = trigger.Query.Select(param => param.Tag.Id).ToArray();
        var tagIds = _dbContext.Tags.Where(tag => queryTagIds.Contains(tag.Id)).ToList();

        if (tagIds.Count != queryTagIds.Length)
        {
            throw new ArgumentException("Some tags in query do not exist.");
        }

        return new CronTriggeredInvocableInfo
        {
            InvocableId = invocableDefinition.Id,
            InvocableType = invocableDefinition.InvocableType,
            CronExpression = trigger.CronExpression,
            TagQuery = trigger.Query
                .Select(param => new TagQueryPart
                {
                    State = trigger.Query.First(queryParam => queryParam.Tag.Id == param.Tag.Id).State,
                    Tag = tagIds.First(tag => tag.Id == param.Tag.Id)
                })
                .ToList(),
            Payload = jsonPayload,
            InvocablePayloadType = payloadExactType
        };
    }

    private static void ValidateInterfaces(
        Type invocableType,
        Type expectedInterfaceType,
        Type expectedPayloadType,
        out Type payloadType)
    {
        var @interface = invocableType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == expectedInterfaceType);

        if (@interface is null)
        {
            throw new ArgumentException($"Invocable {invocableType.Name} does not implement {expectedInterfaceType.Name}");
        }

        payloadType = @interface.GetGenericArguments()[0];

        if (!payloadType.IsAssignableTo(expectedPayloadType))
        {
            throw new ArgumentException($"Payload of event triggered by event must be {expectedPayloadType}");
        }
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
}
