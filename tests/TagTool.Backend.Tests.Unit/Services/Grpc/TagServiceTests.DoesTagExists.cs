using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task DoesTagExists_ValidRequest_TagExists_ReturnsTag()
    {
        // Arrange
        var tag = Any.Pack(_textTag);
        var request = new DoesTagExistsRequest { Tag = tag };

        _mediator.Send(Arg.Any<GetTagQuery>()).Returns(_ => _tagMapper.MapFromDto(tag));

        // Act
        var response = await _sut.DoesTagExists(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetTagQuery>());
        response.Tag.Should().NotBeNull().And.Be(tag);
    }

    [Fact]
    public async Task DoesTagExists_ValidRequest_TagDoesNotExists_ReturnsEmpty()
    {
        // Arrange
        var tag = Any.Pack(_textTag);
        var request = new DoesTagExistsRequest { Tag = tag };

        _mediator.Send(Arg.Any<GetTagQuery>()).Returns(_ => (TagBase?)null);

        // Act
        var response = await _sut.DoesTagExists(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetTagQuery>());
        response.ResultCase.Should().Be(DoesTagExistsReply.ResultOneofCase.None);
        response.Tag.Should().BeNull();
    }

    [Fact]
    public async Task DoesTagExists_InvalidRequest_TagIsNull_Throws()
    {
        // Arrange
        var request = new DoesTagExistsRequest();

        // Act
        var act = () => _sut.DoesTagExists(request, _testServerCallContext);

        // Assert
        await _mediator.DidNotReceive().Send(Arg.Any<GetTagQuery>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage($"*{nameof(request.Tag)}*");
    }
}
