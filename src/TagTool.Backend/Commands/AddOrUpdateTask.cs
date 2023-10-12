using System.Diagnostics;
using Hangfire;
using OneOf;
using TagTool.Backend.Actions;
using TagTool.Backend.Events;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class AddOrUpdateTaskRequest : ICommand<OneOf<string, ErrorResponse>>
{
    public required string TaskId { get; init; }

    public required TagQuery TagQuery { get; init; }

    public required string ActionId { get; init; }

    public required Dictionary<string, string> ActionAttributes { get; init; }

    public required Trigger[] Triggers { get; init; }
}

public class AddOrUpdateTask : ICommandHandler<AddOrUpdateTaskRequest, OneOf<string, ErrorResponse>>
{
    private readonly IActionFactory _actionFactory;
    private readonly IEventTriggersManager _triggersManager;

    private readonly Dictionary<string, Type> _notificationMappings = new() { { "ItemTagged", typeof(ItemTaggedNotif) } };

    public AddOrUpdateTask(IActionFactory actionFactory, IEventTriggersManager triggersManager)
    {
        _actionFactory = actionFactory;
        _triggersManager = triggersManager;
    }

    public Task<OneOf<string, ErrorResponse>> Handle(AddOrUpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var action = _actionFactory.Create(request.ActionId);

        if (action is null)
        {
            // todo: add success/failure messages to reply
            return Task.FromResult((OneOf<string, ErrorResponse>)new ErrorResponse($"Could not create an action for id {request.ActionId}"));
        }

        // var tagQueryMapped = request.QueryParams
        //     .Select(param => new TagQuerySegment { Tag = _tagMapper.MapFromDto(param.Tag), State = MapQuerySegmentStateFromDto(param) })
        //     .ToArray();

        // var tagQuery = new TagQuery { QuerySegments = tagQueryMapped };

        // var jobAttributes = request.ActionAttributes.Values.ToDictionary(pair => pair.Key, pair => pair.Value);

        RecurringJob.AddOrUpdate(request.TaskId, () => action.ExecuteOnSchedule(request.TagQuery, request.ActionAttributes), Cron.Never);

        foreach (var triggerInfo in request.Triggers)
        {
            switch (triggerInfo.Type)
            {
                case Models.TriggerType.Manual:
                    break;
                case Models.TriggerType.Cron:
                    RecurringJob.AddOrUpdate(
                        request.TaskId,
                        () => action.ExecuteOnSchedule(request.TagQuery, request.ActionAttributes),
                        triggerInfo.Arg);
                    break;
                case Models.TriggerType.Event:
                    if (_notificationMappings.TryGetValue(triggerInfo.Arg, out var type))
                    {
                        _triggersManager.AddTrigger(type, request.TaskId);
                    }
                    else
                    {
                        throw new ArgumentException($"no mapping for notification with name {triggerInfo.Arg}");
                    }

                    break;
                default:
                    throw new UnreachableException();
            }
        }

        return Task.FromResult(OneOf<string, ErrorResponse>.FromT0($"successfully added or updated task with id {request.TaskId}"));
    }
}
