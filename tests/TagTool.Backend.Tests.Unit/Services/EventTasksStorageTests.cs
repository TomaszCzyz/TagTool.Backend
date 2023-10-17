using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class EventTasksStorageTests
{
    private readonly EventTasksStorage _sut;

    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();
    private readonly ILogger<EventTasksStorage> _logger = Substitute.For<ILogger<EventTasksStorage>>();

    public EventTasksStorageTests()
    {
        _sut = new EventTasksStorage(_logger, _dbContext);
    }

    [Fact]
    public void GetAll_TasksStored_Returns()
    {
        // Arrange
        var eventTaskDtos = new List<EventTaskDto>
        {
            new()
            {
                TaskId = "TestTaskId1",
                ActionId = "TestsActionId1",
                Events = new[] { "TestEvent1" }
            },
            new()
            {
                TaskId = "TestTaskId2",
                ActionId = "TestsActionId2",
                Events = new[] { "TestEvent2" }
            }
        };
        var tasksMock = eventTaskDtos.AsQueryable().BuildMockDbSet();

        _dbContext.EventTasks.Returns(tasksMock);

        // Act
        var eventTasks = _sut.GetAll();

        // Assert
        var array = eventTasks.Should().BeAssignableTo<IEnumerable<EventTask>>().Subject.ToArray();
        array.Should().HaveCount(2);
        array.Should().BeEquivalentTo(eventTaskDtos.Select(MapFromDto));
    }

    [Fact]
    public void GetAll_NoTasksStored_ReturnEmptyEnumerable()
    {
        // Arrange
        var tasksMock = new List<EventTaskDto>().AsQueryable().BuildMockDbSet();

        _dbContext.EventTasks.Returns(tasksMock);

        // Act
        var eventTasks = _sut.GetAll();

        // Assert
        eventTasks.Should().BeEmpty();
    }

    [Fact]
    public void AddOrUpdate_ValidTask_DoesNotExists_AddedNewEventTask()
    {
        // Arrange
        var eventTask = new EventTask
        {
            TaskId = "TestTaskId1",
            ActionId = "TestsActionId1",
            Events = new[] { "TestEvent1" }
        };

        var eventTaskDtos = new List<EventTaskDto>();
        var taskDtos = eventTaskDtos.AsQueryable().BuildMockDbSet();

        _dbContext.EventTasks.Returns(taskDtos);
        _dbContext.EventTasks.Find(Arg.Any<EventTask>()).Returns((EventTaskDto?)null);
        _dbContext.EventTasks
            .When(set => set.Add(Arg.Any<EventTaskDto>()))
            .Do(info => eventTaskDtos.Add(info.Arg<EventTaskDto>()));

        // Act
        _sut.AddOrUpdate(eventTask);

        // Assert;
        eventTaskDtos.Should().HaveCount(1);
        eventTaskDtos[0].Should().BeEquivalentTo(eventTask);
    }

    [Fact]
    public void AddOrUpdate_ValidTask_Exists_UpdatedEventTask()
    {
        // Arrange
        var oldEventTask = new EventTask
        {
            TaskId = "TestTaskId1",
            ActionId = "TestsActionId1",
            Events = new[] { "TestEvent1" }
        };

        _dbContext.EventTasks.Find(Arg.Any<string>()).Returns(MapToDto(oldEventTask));

        // Act
        _sut.AddOrUpdate(oldEventTask);

        // Assert;
        _dbContext.EventTasks.Received(1).Update(Arg.Any<EventTaskDto>());
        _dbContext.EventTasks.DidNotReceive().Add(Arg.Any<EventTaskDto>());
    }

    [Fact]
    public void Remove_TaskIdExists_EventTaskRemoved()
    {
        // Arrange
        var taskId = "taskId";
        var oldEventTask = new EventTask
        {
            TaskId = taskId,
            ActionId = "TestsActionId1",
            Events = new[] { "TestEvent1" }
        };

        _dbContext.EventTasks.Find(Arg.Any<string>()).Returns(MapToDto(oldEventTask));

        // Act
        _sut.Remove(taskId);

        // Assert;
        _dbContext.EventTasks.Received(1).Remove(Arg.Any<EventTaskDto>());
    }

    [Fact]
    public void Remove_TaskIdDoesNotExists_Returns()
    {
        // Arrange
        var taskId = "taskId";

        _dbContext.EventTasks.Find(Arg.Any<string>()).Returns((EventTaskDto?)null);

        // Act
        _sut.Remove(taskId);

        // Assert;
        _dbContext.EventTasks.DidNotReceive().Remove(Arg.Any<EventTaskDto>());
    }

    private static EventTask MapFromDto(EventTaskDto dto)
        => new()
        {
            TaskId = dto.TaskId,
            ActionId = dto.ActionId,
            ActionAttributes = dto.ActionAttributes,
            Events = dto.Events
        };

    private static EventTaskDto MapToDto(EventTask eventTask)
        => new()
        {
            TaskId = eventTask.TaskId,
            ActionId = eventTask.ActionId,
            ActionAttributes = eventTask.ActionAttributes,
            Events = eventTask.Events
        };
}
