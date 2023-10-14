using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using OneOf;
using TagTool.Backend.Commands;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task Redo_ValidRequest_RedoIsSuccessful_ReturnsSuccessMessage()
    {
        // Arrange
        var arbitraryReversibleCommand = new RenameTag { TagName = "test", NewTagName = "testTest" };
        var request = new RedoRequest();

        _commandsHistory.GetRedoCommand().Returns(_ => arbitraryReversibleCommand);

        // Act
        var response = await _sut.Redo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetRedoCommand();
        await _mediator.Received(1).Send(Arg.Any<IReversible>());
        response.RedoCommand.Should().NotBeNull().And.Be(arbitraryReversibleCommand.GetType().ToString());
        response.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Redo_ValidRequest_RedoIsUnsuccessful_RedoCommandNotFound_ReturnsErrorMessage()
    {
        // Arrange
        var request = new RedoRequest();

        _commandsHistory.GetRedoCommand().Returns(_ => null);

        // Act
        var response = await _sut.Redo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetRedoCommand();
        response.ErrorMessage.Should().NotBeNull().And.Be("Nothing to Redo.");
        response.RedoCommand.Should().BeNull();
    }

    [Fact]
    public async Task Redo_ValidRequest_RedoIsUnsuccessful_CommandFailed_ReturnsErrorMessage()
    {
        // Arrange
        var arbitraryReversibleCommand = new TagTool.Backend.Commands.CreateTagRequest { Tag = new TextTag { Text = "Test" } };
        var request = new RedoRequest();

        _commandsHistory.GetRedoCommand().Returns(_ => arbitraryReversibleCommand);
        _mediator.Send(Arg.Any<IReversible>()).Returns(_ => (OneOf<ErrorResponse>)new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.Redo(request, _testServerCallContext);

        // Assert
        _commandsHistory.Received(1).GetRedoCommand();
        await _mediator.Received(1).Send(Arg.Any<IReversible>());
        response.ErrorMessage.Should().NotBeNull();
        response.RedoCommand.Should().BeNull();
    }
}
