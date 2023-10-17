using Hangfire;
using JetBrains.Annotations;
using TagTool.Backend.Actions;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

[UsedImplicitly]
public class CronTasksManager : ITasksManager<CronTask>
{
    private readonly IActionFactory _actionFactory;
    private readonly IRecurringJobManagerV2 _recurringJobManager;

    public CronTasksManager(IActionFactory actionFactory, IRecurringJobManagerV2 recurringJobManager)
    {
        _actionFactory = actionFactory;
        _recurringJobManager = recurringJobManager;
    }

    public Task<bool> AddOrUpdate(CronTask task)
    {
        // todo: validation
        var action = _actionFactory.Create(task.ActionId);
        if (action is null)
        {
            return Task.FromResult(false);
        }

        _recurringJobManager.AddOrUpdate(
            task.TaskId,
            () => action.ExecuteOnSchedule(task.TagQuery, task.ActionAttributes),
            task.Cron);

        return Task.FromResult(true);
    }

    public void Remove(string taskId)
    {
        _recurringJobManager.RemoveIfExists(taskId);
    }
}
