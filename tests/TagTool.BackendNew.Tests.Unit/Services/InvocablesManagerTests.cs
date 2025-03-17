using Coravel.Scheduling.Schedule.Interfaces;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using TagTool.Backend.Models.Tags;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Models;
using TagTool.BackendNew.Services;
using TagTool.BackendNew.Tests.Unit.Utilities;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;
using TagBase = TagTool.BackendNew.Entities.TagBase;

namespace TagTool.BackendNew.Tests.Unit.Services;

public class InvocablesManagerTests
{
    private InvocablesManager _sut;

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IScheduler _scheduler = Substitute.For<IScheduler>();
    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();

    private readonly string _testJsonPayload;
    private readonly InvocableDefinition _testInvocableDefinition;

    public InvocablesManagerTests()
    {
        _testJsonPayload = JsonSerializer.Serialize(new TestInvocablePayload
        {
            TagQuery =
            [
                new TagQueryPart
                {
                    Id = 5,
                    Tag = new TagBase
                    {
                        Text = "testTagText"
                    }
                }
            ]
        });
        _testInvocableDefinition = new InvocableDefinition(
            "testId",
            "testGroupId",
            "testDisplayName",
            "testDescription",
            _testJsonPayload,
            TriggerType.Cron,
            typeof(TestInvocable)
        );

        _sut = new InvocablesManager(_serviceProvider, _scheduler, _dbContext, [_testInvocableDefinition]);
    }

    [Fact]
    public void GetInvocableDefinitions_ReturnsInvocables()
    {
        // Arrange

        // Act
        var result = _sut.GetInvocableDefinitions();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo((InvocableDefinition[]) [_testInvocableDefinition]);
    }

    [Fact]
    public async Task AddAndActivateInvocable_CronInvocableWitValidPayload_InvocableAddedAndScheduled()
    {
        // Arrange
        var invocableDescriptor = new InvocableDescriptor
        {
            InvocableId = "testId",
            Trigger = new CronTrigger
            {
                CronExpression = "* * * * *",
                Query = [new TagQueryParam { TagId = 5 }]
            },
            Args = _testJsonPayload
        };

        // Act
        await _sut.AddAndActivateInvocable(invocableDescriptor, CancellationToken.None);

        // Assert
        _dbContext.CronTriggeredInvocableInfos.Received(1).Add(Arg.Any<CronTriggeredInvocableInfo>());
        _scheduler.Received(1).ScheduleInvocableType(Arg.Any<Type>());
    }
}
