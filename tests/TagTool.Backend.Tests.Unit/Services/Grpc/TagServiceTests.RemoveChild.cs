using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task RemoveChild_ValidRequest_RemovingSucceed_ReturnsSuccessMessage()
    {
        // Arrange
        var request = new RemoveChildRequest { ChildTag = Any.Pack(_dayTag), ParentTag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.RemoveChildRequest>()).Returns(_ => ArbitrarySuccessMessage);

        // Act
        var response = await _sut.RemoveChild(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.RemoveChildRequest>());
        response.SuccessMessage.Should().Be(ArbitrarySuccessMessage);
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task RemoveChild_ValidRequest_RemovingFailed_ReturnsError()
    {
        // Arrange
        var request = new RemoveChildRequest { ChildTag = Any.Pack(_dayTag), ParentTag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.RemoveChildRequest>()).Returns(_ => new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.RemoveChild(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.RemoveChildRequest>());
        response.Error.Should().NotBeNull();
        response.Error.Message.Should().Be(ArbitraryErrorMessage);
        response.SuccessMessage.Should().BeNull();
    }

    [Fact]
    public async Task RemoveChild_InvalidRequest_ChildTagIsNull_Throws()
    {
        // Arrange
        var request = new RemoveChildRequest { ParentTag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.RemoveChild(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.ChildTag)}*");
    }

    [Fact]
    public async Task RemoveChild_InvalidRequest_ParentTagIsNull_Throws()
    {
        // Arrange
        var request = new RemoveChildRequest { ChildTag = Any.Pack(_dayTag) };

        // Act
        var act = () => _sut.RemoveChild(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.ParentTag)}*");
    }
}
