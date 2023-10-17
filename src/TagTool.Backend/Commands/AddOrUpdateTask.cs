using Hangfire.Annotations;
using OneOf;
using TagTool.Backend.Actions;
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

[UsedImplicitly]
public class AddOrUpdateTask : ICommandHandler<AddOrUpdateTaskRequest, OneOf<string, ErrorResponse>>
{
    private readonly IActionFactory _actionFactory;
    private readonly ITasksManager<EventTask> _eventTasksManager;
    private readonly ITasksManager<CronTask> _cronTasksManager;

    public AddOrUpdateTask(IActionFactory actionFactory, ITasksManager<CronTask> cronTasksManager, ITasksManager<EventTask> eventTasksManager)
    {
        _actionFactory = actionFactory;
        _eventTasksManager = eventTasksManager;
        _cronTasksManager = cronTasksManager;
    }

    public Task<OneOf<string, ErrorResponse>> Handle(AddOrUpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var actionAvailable = _actionFactory
            .GetAvailableActions()
            .Select(info => info.Id)
            .Contains(request.ActionId);

        if (!actionAvailable)
        {
            // todo: add success/failure messages to reply
            return Task.FromResult((OneOf<string, ErrorResponse>)new ErrorResponse($"Action with id {request.ActionId} is not defined"));
        }

        AddOrUpdateCronTask(request);
        AddOrUpdateEventTask(request);

        return Task.FromResult(OneOf<string, ErrorResponse>.FromT0($"successfully added or updated task with id {request.TaskId}"));
    }

    private void AddOrUpdateEventTask(AddOrUpdateTaskRequest request)
    {
        var events = request.Triggers
            .Where(trigger => trigger.Type is Models.TriggerType.Event)
            .Select(trigger => trigger.Arg!) // todo: add validation
            .ToArray();

        if (events.Length != 0)
        {
            var eventTask = new EventTask
            {
                TaskId = request.TaskId,
                ActionId = request.ActionId,
                ActionAttributes = request.ActionAttributes,
                Events = events
            };

            _eventTasksManager.AddOrUpdate(eventTask);
        }
        else
        {
            // If we updating existing task, we have to assume that it had event-triggered task before.
            _eventTasksManager.Remove(request.TaskId);
        }
    }

    private void AddOrUpdateCronTask(AddOrUpdateTaskRequest request)
    {
        var cronTrigger = Array.Find(request.Triggers, trigger => trigger.Type is Models.TriggerType.Cron);

        if (cronTrigger is not null)
        {
            var cronTask = new CronTask
            {
                TaskId = request.TaskId,
                ActionId = request.ActionId,
                ActionAttributes = request.ActionAttributes,
                TagQuery = request.TagQuery,
                Cron = cronTrigger.Arg!
            };

            _cronTasksManager.AddOrUpdate(cronTask);
        }
        else
        {
            // If we updating existing task, we have to assume that it had cron-triggered task before.
            _cronTasksManager.Remove(request.TaskId);
        }
    }
}
