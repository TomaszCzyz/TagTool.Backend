using Hangfire;
using Hangfire.Annotations;
using Hangfire.Storage;
using TagTool.Backend.Actions;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class GetExistingTasksQuery : IQuery<IEnumerable<IJustTask>>;

[UsedImplicitly]
public class GetExistingTasks : IQueryHandler<GetExistingTasksQuery, IEnumerable<IJustTask>>
{
    private readonly ILogger<GetExistingTasks> _logger;
    private readonly IEventTasksStorage _eventTasksStorage;

    public GetExistingTasks(ILogger<GetExistingTasks> logger, IEventTasksStorage eventTasksStorage)
    {
        _logger = logger;
        _eventTasksStorage = eventTasksStorage;
    }

    public Task<IEnumerable<IJustTask>> Handle(GetExistingTasksQuery request, CancellationToken cancellationToken)
    {
        var cronTasks = GetCronTasks();
        var eventTasks = GetEventTasks();

        return Task.FromResult(cronTasks.Concat(eventTasks.Cast<IJustTask>()));
    }

    private IEnumerable<EventTask> GetEventTasks()
    {
        return _eventTasksStorage.GetAll();
    }

    private IEnumerable<CronTask> GetCronTasks()
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        foreach (var recurringJob in recurringJobs)
        {
            var jobArgs = recurringJob.Job.Args;
            if (jobArgs.Count == 2 && jobArgs[0] is TagQuery tagQuery && jobArgs[1] is Dictionary<string, string> data)
            {
                if (!recurringJob.Job.Type.IsAssignableTo(typeof(IAction)))
                {
                    _logger.LogWarning("Recurring job with unknown job type {@RecurringJobDto}", recurringJob);
                }

                // todo: Rework this, because it hurts me eyes.
                var instanceId = (Activator.CreateInstance(recurringJob.Job.Type) as IAction)!.Id;

                yield return new CronTask
                {
                    TaskId = recurringJob.Id,
                    TagQuery = tagQuery,
                    ActionId = instanceId,
                    ActionAttributes = data,
                    Cron = recurringJob.Cron
                };
            }
            else
            {
                _logger.LogWarning("Recurring job with unknown arguments {@RecurringJobDto}", recurringJob);
            }
        }
    }
}
