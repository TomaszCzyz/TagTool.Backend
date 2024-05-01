using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task AddChild_ValidRequest_AddingSucceed_ReturnsSuccessMessage()
    {
        // Arrange
        var request = new AddChildRequest { ChildTag = Any.Pack(_dayTag), ParentTag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.AddChildRequest>()).Returns(_ => ArbitrarySuccessMessage);

        // Act
        var response = await _sut.AddChild(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.AddChildRequest>());
        response.SuccessMessage.Should().Be(ArbitrarySuccessMessage);
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddChild_ValidRequest_AddingFailed_ReturnsError()
    {
        // Arrange
        var request = new AddChildRequest { ChildTag = Any.Pack(_dayTag), ParentTag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.AddChildRequest>()).Returns(_ => new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.AddChild(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.AddChildRequest>());
        response.Error.Should().NotBeNull();
        response.Error.Message.Should().Be(ArbitraryErrorMessage);
        response.SuccessMessage.Should().BeNull();
    }

    [Fact]
    public async Task AddChild_InvalidRequest_ChildTagIsNull_Throws()
    {
        // Arrange
        var request = new AddChildRequest { ParentTag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.AddChild(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.ChildTag)}*");
    }

    [Fact]
    public async Task AddChild_InvalidRequest_ParentTagIsNull_Throws()
    {
        // Arrange
        var request = new AddChildRequest { ChildTag = Any.Pack(_dayTag) };

        // Act
        var act = () => _sut.AddChild(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.ParentTag)}*");
    }
}
