using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.Actions;
using TagTool.Backend.DbContext;
using TagTool.Backend.Events;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

public class EventTasksStorage
{
    private readonly TagToolDbContext _dbContext;

    public EventTasksStorage(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    // // eventName -> list of jobs that are triggered by the event
    // private readonly ConcurrentDictionary<Type, string[]> _tasksTriggersCache = new();
    //
    // public string[] GetTasksForEvent<T>() where T : INotification
    //     => _tasksTriggersCache.TryGetValue(typeof(T), out var taskIds) ? taskIds : Array.Empty<string>();

    public IEnumerable<EventTask> GetAll()
        => _dbContext.EventTasks
            .AsNoTracking()
            .Select(dto
                => new EventTask
                {
                    TaskId = dto.TaskId,
                    ActionId = dto.ActionId,
                    ActionAttributes = dto.ActionAttributes,
                    Events = dto.Events
                })
            .AsEnumerable();

    // todo: async it
    public void AddOrUpdate(EventTask eventTask)
    {
        var eventTaskDto = _dbContext.EventTasks.Find(eventTask.TaskId);
        if (eventTaskDto is not null)
        {
            eventTaskDto.ActionId = eventTask.ActionId;
            eventTaskDto.ActionAttributes = eventTask.ActionAttributes;
            eventTaskDto.Events = eventTask.Events;
            _dbContext.EventTasks.Update(eventTaskDto); // todo: is this call necessary? 
        }

        eventTaskDto = new EventTaskDto
        {
            TaskId = eventTask.TaskId,
            ActionId = eventTask.ActionId,
            ActionAttributes = eventTask.ActionAttributes,
            Events = eventTask.Events
        };

        _dbContext.Add(eventTaskDto);
        _dbContext.SaveChanges();
    }
}

[UsedImplicitly]
public class EventTasksExecutor
{
    private readonly ILogger<EventTasksExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventTasksStorage _eventTasksStorage;

    public EventTasksExecutor(ILogger<EventTasksExecutor> logger, EventTasksStorage eventTasksStorage, IServiceProvider serviceProvider)
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
