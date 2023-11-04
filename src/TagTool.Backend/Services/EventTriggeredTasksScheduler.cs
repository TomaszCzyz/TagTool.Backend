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
    private readonly IEventTasksExecutor _tasksExecutor;

    // todo: consider merging this implementation with EventTasksExecutor 
    public EventTriggeredTasksScheduler(IEventTasksExecutor tasksExecutor)
    {
        _tasksExecutor = tasksExecutor;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        await _tasksExecutor.Run(notification);
    }
}
