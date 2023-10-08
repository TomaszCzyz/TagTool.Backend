using MediatR;

namespace TagTool.Backend.Services;

public class EventTriggeredTasksScheduler<T> : INotificationHandler<T> where T : INotification
{
    private readonly ILogger<EventTriggeredTasksScheduler<T>> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventTriggeredTasksScheduler(ILogger<EventTriggeredTasksScheduler<T>> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(T notification, CancellationToken cancellationToken)
    {
        // await using var serviceScope = _serviceProvider.CreateAsyncScope();
        await RunTasksForNotification();
    }

    private Task RunTasksForNotification()
    {
        _logger.LogInformation("Executing tasks triggered by event {Notification}", typeof(T).Name);
        return Task.CompletedTask;
    }
}
