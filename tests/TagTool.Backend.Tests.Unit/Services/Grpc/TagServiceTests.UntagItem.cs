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
