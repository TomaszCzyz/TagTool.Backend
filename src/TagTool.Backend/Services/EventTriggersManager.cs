using Hangfire;
using JetBrains.Annotations;
using TagTool.Backend.Actions;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

public interface ITasksManager<in TTask> where TTask : IJustTask
{
    Task<bool> AddOrUpdate(TTask task);

    Task<bool> Remove(string taskId);
}

[UsedImplicitly]
public class EventTasksManager : ITasksManager<EventTask>
{
    private readonly EventTasksStorage _eventTasksStorage;

    public EventTasksManager(EventTasksStorage eventTasksStorage)
    {
        _eventTasksStorage = eventTasksStorage;
    }

    public Task<bool> AddOrUpdate(EventTask task)
    {
        _eventTasksStorage.AddOrUpdate(task);

        return Task.FromResult(true);
    }

    public Task<bool> Remove(string taskId) => throw new NotImplementedException();
}

[UsedImplicitly]
public class CronTasksManager : ITasksManager<CronTask>
{
    private readonly IActionFactory _actionFactory;

    public CronTasksManager(IActionFactory actionFactory)
    {
        _actionFactory = actionFactory;
    }

    public Task<bool> AddOrUpdate(CronTask task)
    {
        // todo: validation
        var action = _actionFactory.Create(task.ActionId)!;

        RecurringJob.AddOrUpdate(
            task.TaskId,
            () => action.ExecuteOnSchedule(task.TagQuery, task.ActionAttributes),
            task.Cron);

        return Task.FromResult(true);
    }

    public Task<bool> Remove(string taskId) => throw new NotImplementedException();
}
