using JetBrains.Annotations;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

[UsedImplicitly]
public class EventTasksManager : ITasksManager<EventTask>
{
    private readonly IEventTasksStorage _eventTasksStorage;

    public EventTasksManager(IEventTasksStorage eventTasksStorage)
    {
        _eventTasksStorage = eventTasksStorage;
    }

    public Task<bool> AddOrUpdate(EventTask task)
    {
        _eventTasksStorage.AddOrUpdate(task);

        return Task.FromResult(true);
    }

    public void Remove(string taskId) => _eventTasksStorage.Remove(taskId);
}
