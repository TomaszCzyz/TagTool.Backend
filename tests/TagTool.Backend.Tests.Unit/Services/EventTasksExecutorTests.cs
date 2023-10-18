using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TagTool.Backend.Actions;
using TagTool.Backend.Events;
using TagTool.Backend.Models;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class EventTasksExecutorTests
{
    private readonly EventTasksExecutor _sut;
    private readonly ILogger<EventTasksExecutor> _logger = Substitute.For<ILogger<EventTasksExecutor>>();
    private readonly IEventTasksStorage _eventTasksStorage = Substitute.For<IEventTasksStorage>();

    private readonly IAction _action = Substitute.For<IAction>();

    public EventTasksExecutorTests()
    {
        var serviceCollection = new ServiceCollection();

        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        var serviceScope = Substitute.For<IServiceScope>();
        var actionFactory = Substitute.For<IActionFactory>();

        serviceCollection.AddScoped<IActionFactory>(_ => actionFactory);
        serviceCollection.AddScoped<IServiceScopeFactory>(_ => serviceScopeFactory);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        actionFactory.Create(Arg.Any<string>()).Returns(_action);
        serviceScopeFactory.CreateScope().Returns(serviceScope);
        serviceScope.ServiceProvider.Returns(serviceProvider);

        _sut = new EventTasksExecutor(_logger, _eventTasksStorage, serviceProvider);
    }

    [Fact]
    public async Task Run_ManyTaskAreTriggered_EachActionIsExecuted()
    {
        // Arrange
        var taggableItemId = Guid.NewGuid();
        var item = new ItemTaggedChanged { AddedTagId = 0, TaggableItemId = taggableItemId };
        var storedTasks = new List<EventTask>
        {
            new()
            {
                TaskId = "TaskId1",
                ActionId = "ActionId1",
                Events = new[] { item.EventName }
            },
            new()
            {
                TaskId = "TaskId2",
                ActionId = "ActionId1",
                Events = new[] { item.EventName }
            },
            new()
            {
                TaskId = "TaskId3",
                ActionId = "ActionId2",
                Events = new[] { "EventName1" }
            }
        };

        _eventTasksStorage.GetAll().Returns(storedTasks.AsEnumerable());

        // Act
        await _sut.Run(item);

        // Assert
        await _action.Received(2).ExecuteByEvent(Arg.Is<Guid[]>(ids => ids.Contains(taggableItemId)), Arg.Any<Dictionary<string, string>?>());
    }
}
