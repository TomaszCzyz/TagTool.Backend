using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task DeleteTag_ValidRequest_ReturnsDeletedTag()
    {
        // Arrange
        var any = Any.Pack(_textTag);
        var request = new DeleteTagRequest { Tag = any };
        var domainTag = _tagMapper.MapFromDto(any);

        _mediator.Send(Arg.Any<Backend.Commands.DeleteTagRequest>()).Returns(_ => domainTag);

        // Act
        var response = await _sut.DeleteTag(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.DeleteTagRequest>());
        response.Tag.Should().Be(any);
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ValidRequest_TagNotDeleted_ReturnsErrorMessage()
    {
        // Arrange
        var request = new DeleteTagRequest { Tag = Any.Pack(_textTag) };
        var errorResponse = new ErrorResponse(ArbitraryErrorMessage);

        _mediator.Send(Arg.Any<Backend.Commands.DeleteTagRequest>()).Returns(_ => errorResponse);

        // Act
        var response = await _sut.DeleteTag(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.DeleteTagRequest>());
        response.ErrorMessage.Should().Be(ArbitraryErrorMessage);
        response.Tag.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTag_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new DeleteTagRequest();

        // Act
        var act = () => _sut.DeleteTag(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*Tag*");
    }
}
