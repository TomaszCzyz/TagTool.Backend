using MediatR;
using TagTool.Backend.Events;

namespace TagTool.Backend.Services;

/// <summary>
///     The class that schedules jobs when event occurs.
/// </summary>
/// <remarks>
///     With a current registration, singleton service  will be created for each notification type.
///     It can have performance implication, however for the simplicity the current solution in just fine.
/// </remarks>
public class EventTriggeredTasksScheduler<T> : INotificationHandler<T> where T : TaggableItemChanged
{
    private readonly EventTasksExecutor _tasksExecutor;
    private readonly ILogger<EventTriggeredTasksScheduler<T>> _logger;

    public EventTriggeredTasksScheduler(ILogger<EventTriggeredTasksScheduler<T>> logger, EventTasksExecutor tasksExecutor)
    {
        _logger = logger;
        _tasksExecutor = tasksExecutor;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        await _tasksExecutor.Run(notification);
    }
}
