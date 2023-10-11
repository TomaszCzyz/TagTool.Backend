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
    private readonly ITagMapper _tagMapper = TagMapperHelper.InitializeWithKnownMappers();

    private readonly FileDto _fileDto = new() { Path = "TestItemIdentifier" };
    private readonly NormalTag _textTag = new() { Name = "TestTag1" };

    public TagServiceTests()
    {
        var loggerMock = Substitute.For<ILogger<Backend.Services.Grpc.TagService>>();
        var commandsHistory = Substitute.For<ICommandsHistory>();
        var actionFactory = Substitute.For<IActionFactory>();
        var triggersManager = Substitute.For<IEventTriggersManager>();

        _sut = new Backend.Services.Grpc.TagService(
            loggerMock,
            _mediator,
            commandsHistory,
            _tagMapper,
            actionFactory,
            triggersManager);
    }

    [Fact]
    public async Task TagItem_ValidRequest_ReturnsTaggedItem()
    {
        // Arrange
        var testServerCallContext = TestServerCallContext.Create();
        var tagItemRequest = new TagItemRequest { File = _fileDto, Tag = Any.Pack(_textTag) };

        // TaggableFile??? and it turns out, that it is hard to test it due to weird TaggableItem in-method mapping...

        // _mediator.Send(Arg.Any<IRequest>()).Returns(info => OneOf.OneOf<TaggedItem, ErrorResponse>.FromT0());

        // Act
        var response = await _sut.TagItem(tagItemRequest, testServerCallContext);

        // Assert
    }
}
