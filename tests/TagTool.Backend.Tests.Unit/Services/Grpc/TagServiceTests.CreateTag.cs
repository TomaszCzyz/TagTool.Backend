using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task CreateTag_ValidRequest_ReturnsCreatedTagName()
    {
        // Arrange
        var request = new CreateTagRequest { Tag = Any.Pack(_textTag) };
        var domainTag = _tagMapper.MapFromDto(Any.Pack(_textTag));

        _mediator.Send(Arg.Any<Backend.Commands.CreateTagRequest>()).Returns(_ => domainTag);

        // Act
        var response = await _sut.CreateTag(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.CreateTagRequest>());
        response.Tag.Should().Be(Any.Pack(_textTag));
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CreateTag_ValidRequest_TagNotCreated_ReturnsErrorMessage()
    {
        // Arrange
        var request = new CreateTagRequest { Tag = Any.Pack(_textTag) };
        var errorResponse = new ErrorResponse(ArbitraryErrorMessage);

        _mediator.Send(Arg.Any<Backend.Commands.CreateTagRequest>()).Returns(_ => errorResponse);

        // Act
        var response = await _sut.CreateTag(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.CreateTagRequest>());
        response.ErrorMessage.Should().Be(ArbitraryErrorMessage);
        response.Tag.Should().BeNull();
    }

    [Fact]
    public async Task CreateTag_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new CreateTagRequest();

        // Act
        var act = () => _sut.CreateTag(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*Tag*");
    }
}
