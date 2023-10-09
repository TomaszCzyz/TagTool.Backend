using MediatR;
using TagTool.Backend.Events;
using TagTool.Backend.Jobs;

namespace TagTool.Backend.Services;

/// <summary>
///     The class that schedules jobs when event occurs.
/// </summary>
/// <remarks>
///     With a current registration, singleton service  will be created for each notification type.
///     It can have performance implication, however for the simplicity the current solution in just fine.
/// </remarks>
public class EventTriggeredTasksScheduler<T> : INotificationHandler<T> where T : ITaggableItemNotif
{
    private readonly ILogger<EventTriggeredTasksScheduler<T>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventTriggersManager _eventTriggersManager;
    private readonly IJobFactory _jobFactory;

    public EventTriggeredTasksScheduler(
        ILogger<EventTriggeredTasksScheduler<T>> logger,
        IServiceProvider serviceProvider,
        IEventTriggersManager eventTriggersManager,
        IJobFactory jobFactory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _eventTriggersManager = eventTriggersManager;
        _jobFactory = jobFactory;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        var taskIds = _eventTriggersManager.GetTasksForEvent<T>();
        foreach (var taskId in taskIds)
        {
            _logger.LogInformation("Executing task {TaskId} triggered by event {Notification}", taskId, typeof(T).Name);

            await using var serviceScope = _serviceProvider.CreateAsyncScope();
            var jobId = GetJobForTask();
            var job = _jobFactory.Create(jobId);

            // notification.TaggableItem
            if (job is not null)
            {
                await job.ExecuteByEvent(new[] { notification.TaggableItemId }, new Dictionary<string, string>());
            }
            else
            {
                // log warning
            }

            // RecurringJob.TriggerJob(taskId);
        }
    }

    private string GetJobForTask()
    {
        throw new NotImplementedException();
    }
}
