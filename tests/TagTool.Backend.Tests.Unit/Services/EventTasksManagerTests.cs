using FluentAssertions;
using NSubstitute;
using TagTool.Backend.Models;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class EventTasksManagerTests
{
    private readonly EventTasksManager _sut;
    private readonly IEventTasksStorage _eventTasksStorage = Substitute.For<IEventTasksStorage>();

    public EventTasksManagerTests()
    {
        _sut = new EventTasksManager(_eventTasksStorage);
    }

    [Fact]
    public async Task AddOrUpdate_ValidTask_ReturnsTrue()
    {
        // Arrange
        var eventTask = new EventTask
        {
            TaskId = "TestTaskId",
            ActionId = "TestActionId",
            Events = new[] { "TestEventName" }
        };

        // Act
        var updated = await _sut.AddOrUpdate(eventTask);

        // Assert
        updated.Should().BeTrue();
        _eventTasksStorage
            .Received(1)
            .AddOrUpdate(Arg.Is<EventTask>(e => e.TaskId == eventTask.TaskId));
    }

    [Fact]
    public void Remove_ValidTaskId_InvokeUnderlyingStorageMethod()
    {
        // Arrange
        var taskId = "TestTaskId";

        // Act
        _sut.Remove(taskId);

        // Assert
        _eventTasksStorage.Received(1).Remove(Arg.Is<string>(s => s == taskId));
    }
}
