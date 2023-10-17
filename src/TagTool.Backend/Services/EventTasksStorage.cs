using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Services;

public interface IEventTasksStorage
{
    IEnumerable<EventTask> GetAll();

    void AddOrUpdate(EventTask eventTask);

    void Remove(string taskId);
}

public class EventTasksStorage : IEventTasksStorage
{
    private readonly ILogger<EventTasksStorage> _logger;
    private readonly ITagToolDbContext _dbContext;

    public EventTasksStorage(ILogger<EventTasksStorage> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

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
        if (eventTaskDto is null)
        {
            eventTaskDto = new EventTaskDto
            {
                TaskId = eventTask.TaskId,
                ActionId = eventTask.ActionId,
                ActionAttributes = eventTask.ActionAttributes,
                Events = eventTask.Events
            };

            _logger.LogInformation("Adding EventTask {@EventTask}", eventTaskDto);
            _dbContext.EventTasks.Add(eventTaskDto);
        }
        else
        {
            eventTaskDto.ActionId = eventTask.ActionId;
            eventTaskDto.ActionAttributes = eventTask.ActionAttributes;
            eventTaskDto.Events = eventTask.Events;

            _logger.LogInformation("Updating EventTask {@EventTask}", eventTaskDto);
            _dbContext.EventTasks.Update(eventTaskDto); // todo: is this call necessary? 
        }

        _dbContext.SaveChanges();
    }

    public void Remove(string taskId)
    {
        var eventTaskDto = _dbContext.EventTasks.Find(taskId);
        if (eventTaskDto is null)
        {
            return;
        }

        _logger.LogInformation("Removing EventTask {@EventTask}", eventTaskDto);
        _dbContext.EventTasks.Remove(eventTaskDto);
        _dbContext.SaveChanges();
    }
}
