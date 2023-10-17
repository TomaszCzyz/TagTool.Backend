using FluentAssertions;
using NSubstitute;
using TagTool.Backend.Actions;
using TagTool.Backend.Commands;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Commands;

public class AddOrUpdateTaskTests
{
    private const string TaskId = "taskId";
    private const string ActionId = "actionId";

    private readonly AddOrUpdateTask _sut;

    private readonly IActionFactory _actionFactory = Substitute.For<IActionFactory>();
    private readonly ITasksManager<EventTask> _eventTasksManager = Substitute.For<ITasksManager<EventTask>>();
    private readonly ITasksManager<CronTask> _cronTasksManager = Substitute.For<ITasksManager<CronTask>>();

    private readonly Trigger _cronTrigger = new() { Type = Models.TriggerType.Cron, Arg = "* * * * *" };
    private readonly Trigger _eventTrigger = new() { Type = Models.TriggerType.Event, Arg = "TestEventName" };
    private readonly Actions.ActionInfo _actionInfo = new(ActionId, "TestDescription", new Dictionary<string, string>(), Array.Empty<ItemTypeTag>());
    private readonly TagQuery _tagQuery = new() { QuerySegments = new[] { new TagQuerySegment { Tag = new MonthTag() } } };

    public AddOrUpdateTaskTests()
    {
        _sut = new AddOrUpdateTask(_actionFactory, _cronTasksManager, _eventTasksManager);
    }

    [Fact]
    public async Task Handle_ValidRequest_NoAvailableJob_ReturnsErrorResponse()
    {
        // Arrange
        var command = new Backend.Commands.AddOrUpdateTaskRequest
        {
            TaskId = "taskId",
            TagQuery = new TagQuery { QuerySegments = new[] { new TagQuerySegment { Tag = new MonthTag() } } },
            ActionId = "actionId",
            Triggers = Array.Empty<Trigger>()
        };
        _actionFactory.GetAvailableActions().Returns(_ => new List<Actions.ActionInfo>().AsReadOnly());

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().BeOfType<ErrorResponse>()
            .And.Subject.As<ErrorResponse>().Message.Should().NotBeNull()
            .And.Be($"Action with id {command.ActionId} is not defined");
    }

    [Fact]
    public async Task Handle_ValidRequest_ContainsTriggers_AddMethodsAreInvoked()
    {
        // Arrange
        var command = new Backend.Commands.AddOrUpdateTaskRequest
        {
            TaskId = TaskId,
            TagQuery = _tagQuery,
            ActionId = ActionId,
            Triggers = new[] { _cronTrigger, _eventTrigger }
        };

        _actionFactory.GetAvailableActions().Returns(_ => new List<Actions.ActionInfo> { _actionInfo });

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        _eventTasksManager.DidNotReceive().Remove(Arg.Any<string>());
        await _eventTasksManager
            .Received(1)
            .AddOrUpdate(Arg.Is<EventTask>(
                e => e.TaskId == TaskId
                     && e.ActionId == ActionId
                     && e.Events.Length == 1
                     && e.Events[0] == _eventTrigger.Arg));

        _cronTasksManager.DidNotReceiveWithAnyArgs().Remove(default!);
        await _cronTasksManager
            .Received(1)
            .AddOrUpdate(Arg.Is<CronTask>(
                e => e.TaskId == TaskId
                     && e.ActionId == ActionId
                     && e.Cron == _cronTrigger.Arg
                     && e.TagQuery == _tagQuery));

        response.Value.Should().BeOfType<string>()
            .And.Subject.As<string>().Should().Be($"successfully added or updated task with id {command.TaskId}");
    }

    [Fact]
    public async Task Handle_ValidRequest_DoesNotContainCronTrigger_CorrectMethodsAreInvoked()
    {
        // Arrange
        var command = new Backend.Commands.AddOrUpdateTaskRequest
        {
            TaskId = TaskId,
            TagQuery = _tagQuery,
            ActionId = ActionId,
            Triggers = new[] { _eventTrigger }
        };

        _actionFactory.GetAvailableActions().Returns(_ => new List<Actions.ActionInfo> { _actionInfo });

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        _eventTasksManager.DidNotReceiveWithAnyArgs().Remove(default!);
        await _eventTasksManager
            .Received(1)
            .AddOrUpdate(Arg.Is<EventTask>(
                e => e.TaskId == TaskId
                     && e.ActionId == ActionId
                     && e.Events.Length == 1
                     && e.Events[0] == _eventTrigger.Arg));

        await _cronTasksManager.DidNotReceiveWithAnyArgs().AddOrUpdate(default!);
        _cronTasksManager.Received(1).Remove(Arg.Is<string>(e => e == TaskId));

        response.Value.Should().BeOfType<string>()
            .And.Subject.As<string>().Should().Be($"successfully added or updated task with id {command.TaskId}");
    }

    [Fact]
    public async Task Handle_ValidRequest_DoesNotContainCronTrigger_TaskManagerRemoveMethodsAreInvoked()
    {
        // Arrange
        var command = new Backend.Commands.AddOrUpdateTaskRequest
        {
            TaskId = TaskId,
            TagQuery = _tagQuery,
            ActionId = ActionId,
            Triggers = Array.Empty<Trigger>()
        };

        _actionFactory.GetAvailableActions().Returns(_ => new List<Actions.ActionInfo> { _actionInfo });

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _eventTasksManager.DidNotReceiveWithAnyArgs().AddOrUpdate(default!);
        _eventTasksManager.Received(1).Remove(Arg.Is<string>(e => e == TaskId));

        await _cronTasksManager.DidNotReceiveWithAnyArgs().AddOrUpdate(default!);
        _cronTasksManager.Received(1).Remove(Arg.Is<string>(e => e == TaskId));

        response.Value.Should().BeOfType<string>()
            .And.Subject.As<string>().Should().Be($"successfully added or updated task with id {command.TaskId}");
    }
}
