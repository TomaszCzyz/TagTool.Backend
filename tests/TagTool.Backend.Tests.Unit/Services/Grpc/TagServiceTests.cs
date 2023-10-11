using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TagTool.Backend.Actions;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Services;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public class TagServiceTests
{
    private readonly Backend.Services.Grpc.TagService _sut;

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICommandsHistory _commandsHistory = Substitute.For<ICommandsHistory>();
    private readonly ITagMapper _tagMapper = Substitute.For<ITagMapper>();
    private readonly IActionFactory _actionFactory = Substitute.For<IActionFactory>();
    private readonly IEventTriggersManager _triggersManager = Substitute.For<IEventTriggersManager>();

    public TagServiceTests()
    {
        var loggerMock = Substitute.For<ILogger<Backend.Services.Grpc.TagService>>();

        _sut = new Backend.Services.Grpc.TagService(
            loggerMock,
            _mediator,
            _commandsHistory,
            _tagMapper,
            _actionFactory,
            _triggersManager);
    }

    [Fact]
    public async Task TagItem_ValidRequest_ReturnsTaggedItem()
    {
        // Arrange
        var testServerCallContext = TestServerCallContext.Create();
        var fileDto = new FileDto { Path = "TestItemIdentifier" };
        var yearTag = new YearTagDto { Year = 4022 };
        var tagItemRequest = new TagItemRequest { File = fileDto, Tag = Any.Pack(yearTag) };

        // Act
        var response = await _sut.TagItem(tagItemRequest, testServerCallContext);

        // Assert
    }
}
