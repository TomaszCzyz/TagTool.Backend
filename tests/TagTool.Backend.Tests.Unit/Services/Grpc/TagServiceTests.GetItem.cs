using FluentAssertions;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using TagTool.Backend.Queries;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task GetItem_ValidRequestWithId_ReturnsTaggableItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var taggableItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var request = new GetItemRequest { Id = Guid.NewGuid().ToString() };

        _mediator.Send(Arg.Any<GetItemByIdQuery>()).Returns(_ => taggableItem);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemQuery>());
        response.TaggedItem.Should().NotBeNull();
        response.TaggedItem.TaggableItem.Should().Be(taggableItemDto);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_ValidRequestWithIdAndItem_ReturnsTaggableItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var taggableItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var request = new GetItemRequest { Id = Guid.NewGuid().ToString(), TaggableItemDto = taggableItemDto };

        _mediator.Send(Arg.Any<GetItemByIdQuery>()).Returns(_ => taggableItem);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemQuery>());
        response.TaggedItem.Should().NotBeNull();
        response.TaggedItem.TaggableItem.Should().Be(taggableItemDto);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_ValidRequestWithoutId_ReturnsTaggableItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var taggableItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var request = new GetItemRequest { TaggableItemDto = taggableItemDto };

        _mediator.Send(Arg.Any<GetItemQuery>()).Returns(_ => taggableItem);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.Received(1).Send(Arg.Any<GetItemQuery>());
        response.TaggedItem.Should().NotBeNull();
        response.TaggedItem.TaggableItem.Should().Be(taggableItemDto);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_ValidRequestWithId_NoItemFound_ReturnsErrorMessage()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();

        var request = new GetItemRequest { Id = id };

        _mediator.Send(Arg.Any<GetItemByIdQuery>()).Returns(_ => (TaggableItem?)null);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemQuery>());
        response.ErrorMessage.Should().NotBeNull().And.Be($"Could not find taggable item with id {request.Id}.");
        response.TaggedItem.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_ValidRequestWithoutId_NoItemFound_ReturnsErrorMessage()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };

        var request = new GetItemRequest { TaggableItemDto = taggableItemDto };

        _mediator.Send(Arg.Any<GetItemQuery>()).Returns(_ => (TaggableItem?)null);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.Received(1).Send(Arg.Any<GetItemQuery>());
        response.ErrorMessage.Should().NotBeNull().And.Be($"Could not find taggable item {request.TaggableItemDto} in a database.");
        response.TaggedItem.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_InvalidRequest_IncorrectIdFormatButCorrectItem_ReturnsTaggableItem()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var taggableItem = _taggableItemMapper.MapFromDto(taggableItemDto);

        var request = new GetItemRequest { Id = "not_guid", TaggableItemDto = taggableItemDto };

        _mediator.Send(Arg.Any<GetItemQuery>()).Returns(_ => taggableItem);

        // Act
        var response = await _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.Received(1).Send(Arg.Any<GetItemQuery>());
        response.TaggedItem.Should().NotBeNull();
        response.TaggedItem.TaggableItem.Should().Be(taggableItemDto);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetItem_InvalidRequest_IncorrectIdAndItemIsNull_Throws()
    {
        // Arrange
        var request = new GetItemRequest { Id = "not_guid" };

        // Act
        var act = () => _sut.GetItem(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemByIdQuery>());
        await _mediator.DidNotReceive().Send(Arg.Any<GetItemQuery>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.TaggableItemDto)}*");
    }
}
