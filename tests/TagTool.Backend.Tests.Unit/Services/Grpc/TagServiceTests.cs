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

    // todo: add tests for InvalidRequests
    [Fact]
    public async Task TagItem_ValidRequest_TaggedSuccessfully_ReturnsTaggedItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var existingItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var request = new TagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator
            .When(mediator => mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()))
            .Do(_ => existingItem.Tags.Add(_tagMapper.MapFromDto(Any.Pack(_textTag))));
        _mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()).Returns(_ => existingItem);

        // Act
        var response = await _sut.TagItem(request, _testServerCallContext);

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

        var request = new TagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.TagItemRequest>()).Returns(_ => errorResponse);

        // Act
        var response = await _sut.TagItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.TagItemRequest>());
        response.ErrorMessage.Should().Be(ArbitraryErrorMessage);
        response.Item.Should().BeNull();
    }

    [Fact]
    public async Task TagItem_InvalidRequest_ItemIsNull_Throws()
    {
        // Arrange
        var request = new TagItemRequest { Tag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.TagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TagItem_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new TagItemRequest { Item = new TaggableItemDto() };

        // Act
        var act = () => _sut.TagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UntagItem_ValidRequest_UntaggedSuccessfully_ReturnsTaggedItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var tagBase = _tagMapper.MapFromDto(Any.Pack(_textTag));
        var existingItem = _taggableItemMapper.MapFromDto(taggableItemDto);
        existingItem.Tags.Add(tagBase);

        var request = new UntagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator
            .When(mediator => mediator.Send(Arg.Any<Backend.Commands.UntagItemRequest>()))
            .Do(_ => existingItem.Tags.Remove(tagBase));
        _mediator.Send(Arg.Any<Backend.Commands.UntagItemRequest>()).Returns(_ => existingItem);

        // Act
        var response = await _sut.UntagItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.UntagItemRequest>());
        response.TaggedItem.Should().NotBeNull();
        response.TaggedItem.TaggableItem.Should().Be(taggableItemDto);
        response.TaggedItem.Tags.Should().NotContain(Any.Pack(_textTag));
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task UntagItem_ValidRequest_UntaggedUnsuccessfully_ReturnsError()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var errorResponse = new ErrorResponse(ArbitraryErrorMessage);

        var request = new UntagItemRequest { Item = taggableItemDto, Tag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.UntagItemRequest>()).Returns(_ => errorResponse);

        // Act
        var response = await _sut.UntagItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.UntagItemRequest>());
        response.ErrorMessage.Should().Be(ArbitraryErrorMessage);
        response.TaggedItem.Should().BeNull();
    }

    [Fact]
    public async Task UntagItem_InvalidRequest_ItemIsNull_Throws()
    {
        // Arrange
        var request = new UntagItemRequest { Tag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.UntagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UntagItem_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new UntagItemRequest { Item = new TaggableItemDto() };

        // Act
        var act = () => _sut.UntagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
