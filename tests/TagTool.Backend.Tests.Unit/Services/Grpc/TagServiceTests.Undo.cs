using FluentAssertions;
using NSubstitute;
using OneOf;
using TagTool.Backend.Commands;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task Undo_ValidRequest_UndoIsSuccessful_ReturnsSuccessMessage()
    {
        // Arrange
        var arbitraryReversibleCommand = new RenameTag { TagName = "test", NewTagName = "testTest" };
        var request = new UndoRequest();

        _commandsHistory.GetUndoCommand().Returns(_ => arbitraryReversibleCommand);

        // Act
        var response = await _sut.Undo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetUndoCommand();
        await _mediator.Received(1).Send(Arg.Any<IReversible>());
        response.UndoCommand.Should().NotBeNull().And.Be(arbitraryReversibleCommand.GetType().ToString());
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Undo_ValidRequest_UndoIsUnsuccessful_UndoCommandNotFound_ReturnsErrorMessage()
    {
        // Arrange
        var request = new UndoRequest();

        _commandsHistory.GetUndoCommand().Returns(_ => null);

        // Act
        var response = await _sut.Undo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetUndoCommand();
        response.ErrorMessage.Should().NotBeNull().And.Be("Nothing to Undo.");
        response.UndoCommand.Should().BeNull();
    }

    [Fact]
    public async Task Undo_ValidRequest_UndoIsUnsuccessful_CommandFailed_ReturnsErrorMessage()
    {
        // Arrange
        var arbitraryReversibleCommand = new Backend.Commands.CreateTagRequest { Tag = new TextTag { Text = "Test" } };
        var request = new UndoRequest();

        _commandsHistory.GetUndoCommand().Returns(_ => arbitraryReversibleCommand);
        _mediator.Send(Arg.Any<IReversible>()).Returns(_ => (OneOf<ErrorResponse>)new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.Undo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetUndoCommand();
        await _mediator.Received(1).Send(Arg.Any<IReversible>());
        response.ErrorMessage.Should().NotBeNull();
        response.UndoCommand.Should().BeNull();
    }
}
