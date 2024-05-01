using FluentAssertions;
using NSubstitute;
using TagTool.Backend.Commands;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task ExecuteLinkedAction_ValidRequest_Succeed_ReturnsSuccessMessage()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var request = new ExecuteLinkedActionRequest { Item = taggableItemDto };

        _mediator.Send(Arg.Any<ExecuteLinkedRequest>()).Returns(_ => ArbitrarySuccessMessage);

        // Act
        var response = await _sut.ExecuteLinkedAction(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<ExecuteLinkedRequest>());
        response.Error.Should().BeNull();
        response.ResultCase.Should().Be(ExecuteLinkedActionReply.ResultOneofCase.None);
    }

    [Fact]
    public async Task ExecuteLinkedAction_ValidRequest_Failed_ReturnsError()
    {
        // Arrange
        var taggableItemDto = new TaggableItemDto { File = _fileDto };
        var request = new ExecuteLinkedActionRequest { Item = taggableItemDto };

        _mediator.Send(Arg.Any<ExecuteLinkedRequest>()).Returns(_ => new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.ExecuteLinkedAction(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<ExecuteLinkedRequest>());
        response.ResultCase.Should().Be(ExecuteLinkedActionReply.ResultOneofCase.Error);
        response.Error.Should().NotBeNull();
        response.Error.Message.Should().NotBeNull().And.Be(ArbitraryErrorMessage);
    }

    [Fact]
    public async Task ExecuteLinkedAction_InvalidRequest_ItemIsNull_Throws()
    {
        // Arrange
        var request = new ExecuteLinkedActionRequest();

        // Act
        var act = () => _sut.ExecuteLinkedAction(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<ExecuteLinkedRequest>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.Item)}*");
    }
}
