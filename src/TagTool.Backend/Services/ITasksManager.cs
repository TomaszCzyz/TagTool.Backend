using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

public interface ITasksManager<in TTask> where TTask : IJustTask
{
    Task<bool> AddOrUpdate(TTask task);

    void Remove(string taskId);
}
