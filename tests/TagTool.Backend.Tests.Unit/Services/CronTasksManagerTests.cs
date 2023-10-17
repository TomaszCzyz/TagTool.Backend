using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using NSubstitute;
using TagTool.Backend.Actions;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class CronTasksManagerTests
{
    private readonly CronTasksManager _sut;
    private readonly IActionFactory _actionFactory = Substitute.For<IActionFactory>();
    private readonly IRecurringJobManagerV2 _recurringJobManager = Substitute.For<IRecurringJobManagerV2>();

    private readonly TagQuery _tagQuery = new() { QuerySegments = new[] { new TagQuerySegment { Tag = new MonthTag() } } };

    public CronTasksManagerTests()
    {
        _sut = new CronTasksManager(_actionFactory, _recurringJobManager);
    }

    [Fact]
    public async Task AddOrUpdate_ValidTask_ReturnsTrue()
    {
        // Arrange
        var cronTask = new CronTask
        {
            TaskId = "TestTaskId",
            ActionId = "TestActionId",
            ActionAttributes = new Dictionary<string, string>(),
            TagQuery = _tagQuery,
            Cron = "* * * * *"
        };

        _actionFactory.Create(Arg.Any<string>()).Returns(new MoveAction());
        _recurringJobManager.AddOrUpdate(
            Arg.Any<string>(),
            Arg.Any<Job>(),
            Arg.Any<string>(),
            Arg.Any<RecurringJobOptions>());

        // Act
        var updated = await _sut.AddOrUpdate(cronTask);

        // Assert
        updated.Should().BeTrue();
        _actionFactory.Received(1).Create(Arg.Any<string>());
        _recurringJobManager
            .Received(1)
            .AddOrUpdate(
                Arg.Is<string>(s => s == cronTask.TaskId),
                Arg.Any<Job>(),
                Arg.Is<string>(s => s == cronTask.Cron),
                Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task AddOrUpdate_IncorrectTask_ActionIdDoesNotExists_ReturnsFalse()
    {
        // Arrange
        var cronTask = new CronTask
        {
            TaskId = "TestTaskId",
            ActionId = "TestActionId",
            ActionAttributes = new Dictionary<string, string>(),
            TagQuery = _tagQuery,
            Cron = "* * * * *"
        };

        _actionFactory.Create(Arg.Any<string>()).Returns((IAction?)null);

        // Act
        var updated = await _sut.AddOrUpdate(cronTask);

        // Assert
        updated.Should().BeFalse();
        _actionFactory.Received(1).Create(Arg.Any<string>());
        _recurringJobManager.DidNotReceive().AddOrUpdate(default, default, default, default);
    }
}
