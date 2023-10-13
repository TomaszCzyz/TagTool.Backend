using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Services;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public class TagServiceTests
{
    private const string ArbitraryErrorMessage = "Arbitrary error message.";

    private readonly Backend.Services.Grpc.TagService _sut;

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ITagMapper _tagMapper = TagMapperHelper.InitializeWithKnownMappers();
    private readonly ITaggableItemMapper _taggableItemMapper = new TaggableItemMapper();
    private readonly TestServerCallContext _testServerCallContext = TestServerCallContext.Create();

    private readonly FileDto _fileDto = new() { Path = "TestItemIdentifier" };
    private readonly NormalTag _textTag = new() { Name = "TestTag1" };

    public TagServiceTests()
    {
        var loggerMock = Substitute.For<ILogger<Backend.Services.Grpc.TagService>>();
        var commandsHistory = Substitute.For<ICommandsHistory>();

        _sut = new Backend.Services.Grpc.TagService(
            loggerMock,
            _mediator,
            commandsHistory,
            _tagMapper,
            _taggableItemMapper);
    }

    [Fact]
    public async Task TagItem_ValidRequest_TaggedSuccessfully_ReturnsTaggedItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var existingItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var tagItemRequest = new TagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator
            .When(mediator => mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()))
            .Do(_ => existingItem.Tags.Add(_tagMapper.MapFromDto(Any.Pack(_textTag))));
        _mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()).Returns(_ => existingItem);

        // Act
        var response = await _sut.TagItem(tagItemRequest, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.TagItemRequest>());
        response.Item.Should().NotBeNull();
        response.Item.TaggableItem.Should().Be(taggableItemDto);
        response.Item.Tags.Should().Contain(Any.Pack(_textTag));
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task TagItem_ValidRequest_TaggedUnsuccessfully_ReturnsError()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var errorResponse = new ErrorResponse(ArbitraryErrorMessage);

        var tagItemRequest = new TagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()).Returns(_ => errorResponse);

        // Act
        var response = await _sut.TagItem(tagItemRequest, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.TagItemRequest>());
        response.ErrorMessage.Should().Be(ArbitraryErrorMessage);
        response.Item.Should().BeNull();
    }
}
