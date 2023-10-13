using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    private const string GroupName = "groupName";

    [Fact]
    public async Task AddSynonym_ValidRequest_AddingSucceed_ReturnsSuccessMessage()
    {
        // Arrange
        var request = new AddSynonymRequest { GroupName = GroupName, Tag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.AddSynonymRequest>()).Returns(_ => ArbitrarySuccessMessage);

        // Act
        var response = await _sut.AddSynonym(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.AddSynonymRequest>());
        response.SuccessMessage.Should().Be(ArbitrarySuccessMessage);
        response.Error.Should().BeNull();
    }

    [Fact]
    public async Task AddSynonym_ValidRequest_AddingFailed_ReturnsError()
    {
        // Arrange
        var request = new AddSynonymRequest { GroupName = GroupName, Tag = Any.Pack(_textTag) };

        _mediator.Send(Arg.Any<Backend.Commands.AddSynonymRequest>()).Returns(_ => new ErrorResponse(ArbitraryErrorMessage));

        // Act
        var response = await _sut.AddSynonym(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<Backend.Commands.AddSynonymRequest>());
        response.Error.Should().NotBeNull();
        response.Error.Message.Should().Be(ArbitraryErrorMessage);
        response.SuccessMessage.Should().BeNull();
    }

    [Fact]
    public async Task AddSynonym_InvalidRequest_GroupNameIsNull_Throws()
    {
        // Arrange
        var request = new AddSynonymRequest { Tag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.AddSynonym(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.GroupName)}*");
    }

    [Fact]
    public async Task AddSynonym_InvalidRequest_GroupNameIsEmpty_Throws()
    {
        // Arrange
        var request = new AddSynonymRequest { Tag = Any.Pack(_textTag) };

        // Act
        var act = () => _sut.AddSynonym(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.GroupName)}*");
    }

    [Fact]
    public async Task AddSynonym_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new AddSynonymRequest { GroupName = GroupName };

        // Act
        var act = () => _sut.AddSynonym(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{nameof(request.Tag)}*");
    }
}
