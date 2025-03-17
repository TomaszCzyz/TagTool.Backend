using System.Text.Json;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using Shouldly;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Services;
using TagTool.BackendNew.Tests.Unit.Utilities;
using Xunit;

namespace TagTool.BackendNew.Tests.Unit.Services;

public class CronInvocableQueuingHandlerTests
{
    private readonly CronInvocableQueuingHandler _sut;

    private readonly ILogger<CronInvocableQueuingHandler> _logger = Substitute.For<ILogger<CronInvocableQueuingHandler>>();
    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly Guid _testInvocableId = Guid.NewGuid();

    public CronInvocableQueuingHandlerTests()
    {
        _sut = new CronInvocableQueuingHandler(_logger, _dbContext, _serviceProvider, _testInvocableId);
    }

    [Fact]
    public async Task Invoke_InvocableNotFound_LogsError()
    {
        // Arrange
        var invocableInfos = Enumerable.Empty<CronTriggeredInvocableInfo>();
        var cronTriggeredInvocableInfos = invocableInfos.AsQueryable().BuildMockDbSet();
        _dbContext.CronTriggeredInvocableInfos.Returns(cronTriggeredInvocableInfos);

        // Act
        await _sut.Invoke();

        // Assert
        _logger.AssertLog(LogLevel.Error, "Invocable not found");
    }

    [Fact]
    public async Task Invoke_InvalidPayload_LogsError()
    {
        // Arrange
        var testInfo = new CronTriggeredInvocableInfo
        {
            Id = _testInvocableId,
            InvocableType = typeof(TestInvocable),
            InvocablePayloadType = typeof(TestInvocablePayload),
            Payload = "invalid_payload",
            CronExpression = "* * * * *",
            TagQuery = []
        };

        var invocableInfos = new List<CronTriggeredInvocableInfo>
        {
            testInfo
        };
        var cronTriggeredInvocableInfos = invocableInfos.AsQueryable().BuildMockDbSet();

        _dbContext.CronTriggeredInvocableInfos.Returns(cronTriggeredInvocableInfos);
        _dbContext.CronTriggeredInvocableInfos.FindAsync(_testInvocableId).Returns(testInfo);

        var queuingHandlerType = typeof(IQueuingHandler<TestInvocable, TestInvocablePayload>);
        var queuingHandler = Substitute.For<IQueuingHandler<TestInvocable, TestInvocablePayload>>();
        _serviceProvider.GetService(queuingHandlerType).Returns(queuingHandler);

        // Act
        await _sut.Invoke();

        // Assert
        _logger.AssertLog(LogLevel.Error, "Unable to deserialize payload");
        queuingHandler.DidNotReceive().Queue(Arg.Any<object>());
    }

    [Fact]
    public async Task Invoke_ValidPayload_QueuesAndLogsInformation()
    {
        // Arrange
        var payloadObject = new TestInvocablePayload
        {
            TagQuery = []
        };

        var testInfo = new CronTriggeredInvocableInfo
        {
            Id = _testInvocableId,
            InvocableType = typeof(TestInvocable),
            InvocablePayloadType = payloadObject.GetType(),
            Payload = JsonSerializer.Serialize(payloadObject),
            CronExpression = "* * * * *",
            TagQuery = []
        };

        _dbContext.CronTriggeredInvocableInfos.FindAsync(_testInvocableId).Returns(testInfo);

        var queuingHandlerType = typeof(IQueuingHandler<TestInvocable, TestInvocablePayload>);
        var queuingHandler = Substitute.For<IQueuingHandler<TestInvocable, TestInvocablePayload>>();
        _serviceProvider.GetService(queuingHandlerType).Returns(queuingHandler);

        // Act
        await _sut.Invoke();

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Queuing cron-triggered invocable")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        queuingHandler.Received(1).Queue(Arg.Any<object>());
    }

    [Fact]
    public void Invoke_WrongQueuingHandler_ThrowsException()
    {
        // Arrange
        var testInfo = new CronTriggeredInvocableInfo
        {
            InvocableType = typeof(TestInvocable),
            InvocablePayloadType = typeof(TestInvocablePayload),
            Payload = JsonSerializer.Serialize(new object()),
            CronExpression = "* * * * *",
            TagQuery = []
        };

        _dbContext.CronTriggeredInvocableInfos.FindAsync(_testInvocableId).Returns(testInfo);

        var queuingHandlerType = typeof(IQueuingHandler<TestInvocable, TestInvocablePayload>);
        _serviceProvider.GetService(queuingHandlerType).Returns(Substitute.For<object>());

        // Act
        var act = () => _sut.Invoke();

        // Assert
        var exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldContain("Incorrect QueuingHandler");
    }
}
