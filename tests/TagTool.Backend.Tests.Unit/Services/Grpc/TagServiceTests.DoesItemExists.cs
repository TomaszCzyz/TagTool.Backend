using FluentAssertions;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Queries;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task DoesItemExists_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };

        var request = new DoesItemExistsRequest { Item = taggableItemDto };

        _mediator.Send(Arg.Any<DoesItemExistsQuery>()).Returns(_ => true);

        // Act
        var response = await _sut.DoesItemExists(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<DoesItemExistsQuery>());
        response.Exists.Should().Be(true);
    }

    [Fact]
    public async Task DoesItemExists_ValidRequest_ReturnsFalse()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };

        var request = new DoesItemExistsRequest { Item = taggableItemDto };

        _mediator.Send(Arg.Any<DoesItemExistsQuery>()).Returns(_ => false);

        // Act
        var response = await _sut.DoesItemExists(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<DoesItemExistsQuery>());
        response.Exists.Should().Be(false);
    }

    [Fact]
    public async Task DoesItemExists_InvalidRequest_ItemIsNull_Throws()
    {
        // Arrange
        var request = new DoesItemExistsRequest();

        // Act
        var act = () => _sut.DoesItemExists(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<DoesItemExistsQuery>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.Item)}*");
    }
}
