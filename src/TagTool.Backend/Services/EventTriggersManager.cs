using System.Collections.Concurrent;
using MediatR;

namespace TagTool.Backend.Services;

public interface IEventTriggersManager
{
    string[] GetTasksForEvent<T>() where T : INotification;

    Task<bool> AddTrigger(Type triggerType, string taskId);
}

public class EventTriggersManager : IEventTriggersManager
{
    // eventName -> list of jobs that are triggered by the event
    private readonly ConcurrentDictionary<Type, string[]> _tasksTriggersCache = new();

    public string[] GetTasksForEvent<T>() where T : INotification
        => _tasksTriggersCache.TryGetValue(typeof(T), out var taskIds) ? taskIds : Array.Empty<string>();

    public Task<bool> AddTrigger(Type triggerType, string taskId)
    {
        // todo: add persistent storage 
        _ = _tasksTriggersCache.AddOrUpdate(
            triggerType,
            _ => new[] { taskId },
            (_, strings) => strings.Append(taskId).ToArray());

        return Task.FromResult(true);
    }

    // private static readonly Dictionary<string, Type> _notificationMappings
    //     = new()
    //     {
    //         { nameof(TagCreatedNotification), typeof(TagCreatedNotification) },
    //         { nameof(TagDeletedNotification), typeof(TagDeletedNotification) },
    //         { nameof(ItemTaggedNotification), typeof(ItemTaggedNotification) },
    //         { nameof(ItemUntaggedNotification), typeof(ItemUntaggedNotification) }
    //     };
}
