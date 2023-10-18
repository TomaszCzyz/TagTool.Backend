using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task GetExistingTasks_ValidRequest_ReturnsAllTasks()
    {
        // Arrange
        var cronTaskWithId1 = new CronTask
        {
            TaskId = "TaskId1",
            ActionId = "ActionId1",
            TagQuery = new TagQuery { QuerySegments = new[] { new TagQuerySegment { Tag = new TextTag { Text = "TestText" } } } },
            Cron = "* * * * *"
        };
        var eventTaskWithId1 = new EventTask
        {
            TaskId = "TaskId1",
            ActionId = "ActionId1",
            Events = new[] { "TestEventName" },
        };
        var eventTaskWithId2 = new EventTask
        {
            TaskId = "TaskId2",
            ActionId = "ActionId2",
            Events = new[] { "TestEventName" },
        };

        var mediatorResponses = new List<IJustTask>
        {
            cronTaskWithId1,
            eventTaskWithId1,
            eventTaskWithId2
        };

        _mediator.Send(Arg.Any<GetExistingTasksQuery>()).Returns(_ => mediatorResponses.AsEnumerable());

        var request = new GetExistingTasksRequest();
        var responseStream = new TestServerStreamWriter<GetExistingTasksReply>(_testServerCallContext);

        // Act
        using var call = _sut.GetExistingTasks(request, responseStream, _testServerCallContext);

        // Assert
        responseStream.Complete();

        var reply1 = await responseStream.ReadNextAsync();
        reply1.Should().NotBeNull();
        Debug.Assert(reply1 != null, nameof(reply1) + " != null");
        reply1.TaskId.Should().Be("TaskId1");
        reply1.ActionId.Should().Be("ActionId1");
        reply1.Triggers.Should().HaveCount(2);

        var reply2 = await responseStream.ReadNextAsync();
        reply2.Should().NotBeNull();
        Debug.Assert(reply2 != null, nameof(reply2) + " != null");
        reply2.TaskId.Should().Be("TaskId2");
        reply2.ActionId.Should().Be("ActionId2");
        reply2.Triggers.Should().HaveCount(1);

        var reply3 = await responseStream.ReadNextAsync();
        reply3.Should().BeNull();

        await _mediator.Received(1).Send(Arg.Any<GetExistingTasksQuery>(), Arg.Any<CancellationToken>());
    }
}
