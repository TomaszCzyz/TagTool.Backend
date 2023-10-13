using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
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
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.Item)}*");
    }

    [Fact]
    public async Task TagItem_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new TagItemRequest { Item = new TaggableItemDto() };

        // Act
        var act = () => _sut.TagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.Tag)}*");
    }

    [Fact]
    public async Task TagItem_InvalidRequest_ItemIsEmpty_Throws()
    {
        // Arrange
        var request = new TagItemRequest { Item = new TaggableItemDto(), Tag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.TagItem(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*dto*");
    }
}
