using JetBrains.Annotations;
using TagTool.Backend.Actions;
using TagTool.Backend.Events;

namespace TagTool.Backend.Services;

public interface IEventTasksExecutor
{
    Task Run(TaggableItemChanged itemChanged);
}

[UsedImplicitly]
public class EventTasksExecutor : IEventTasksExecutor
{
    private readonly ILogger<EventTasksExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEventTasksStorage _eventTasksStorage;

    public EventTasksExecutor(ILogger<EventTasksExecutor> logger, IEventTasksStorage eventTasksStorage, IServiceProvider serviceProvider)
    {
        _eventTasksStorage = eventTasksStorage;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Run(TaggableItemChanged itemChanged)
    {
        var eventTasksForNotif = _eventTasksStorage
            .GetAll()
            .Where(task => task.Events.Contains(itemChanged.EventName));

        var tasks = new List<Task>();
        foreach (var eventTask in eventTasksForNotif)
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var action = serviceScope.ServiceProvider
                .GetRequiredService<IActionFactory>()
                .Create(eventTask.ActionId)!;

            var task = action.ExecuteByEvent(new[] { itemChanged.TaggableItemId }, eventTask.ActionAttributes);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
}
