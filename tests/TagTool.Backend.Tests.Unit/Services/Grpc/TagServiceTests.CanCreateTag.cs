using FluentAssertions;
using NSubstitute;
using OneOf;
using OneOf.Types;
using TagTool.Backend.Models;
using TagTool.Backend.Queries;
using Xunit;
using Error = TagTool.Backend.DomainTypes.Error;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task CanCreateTag_ValidRequests_ReturnsCorrectReplies()
    {
        // Arrange
        var requests = new List<CanCreateTagRequest>
        {
            new() { TagName = "TagNam" },
            new() { TagName = "TagName" },
            new() { TagName = "TagNameOriginalPart" }
        };

        var mediatorResponses = new List<OneOf<ErrorResponse, None>>
        {
            new ErrorResponse(ArbitraryErrorMessage),
            new ErrorResponse(ArbitraryErrorMessage),
            new None()
        };

        var replies = new List<CanCreateTagReply>
        {
            new() { Error = new Error { Message = ArbitraryErrorMessage } },
            new() { Error = new Error { Message = ArbitraryErrorMessage } },
            new()
        };

        var requestStream = new TestAsyncStreamReader<CanCreateTagRequest>(_testServerCallContext);
        var responseStream = new TestServerStreamWriter<CanCreateTagReply>(_testServerCallContext);

        _mediator.Send(Arg.Any<CanCreateTagQuery>()).Returns(mediatorResponses[0], mediatorResponses[1], mediatorResponses[2]);

        // Act
        using var call = _sut.CanCreateTag(requestStream, responseStream, _testServerCallContext);

        // Assert
        for (var i = 0; i < 3; i++)
        {
            requestStream.AddMessage(requests[i]);
            var reply = await responseStream.ReadNextAsync();
            reply.Should().BeEquivalentTo(replies[i]);
        }

        requestStream.Complete();
        await call;
        responseStream.Complete();

        var canCreateTagReply = await responseStream.ReadNextAsync();
        canCreateTagReply.Should().BeNull();
        await _mediator.Received(3).Send(Arg.Any<CanCreateTagQuery>());
    }

    [Fact]
    public async Task CanCreateTag_InvalidRequest_NullTagName_ReturnsCorrectReplies()
    {
        // Arrange
        var requests = new List<CanCreateTagRequest>
        {
            new() { TagName = "TagNam" },
            new(), // null TagName
            new() { TagName = "TagNameOriginalPart" }
        };

        var mediatorResponses = new List<OneOf<ErrorResponse, None>>
        {
            new ErrorResponse(ArbitraryErrorMessage),
            new None()
        };

        var expectedReplies = new List<CanCreateTagReply>
        {
            new() { Error = new Error { Message = ArbitraryErrorMessage } },
            new() { Error = new Error { Message = "Tag name cannot be empty." } },
            new()
        };

        var requestStream = new TestAsyncStreamReader<CanCreateTagRequest>(_testServerCallContext);
        var responseStream = new TestServerStreamWriter<CanCreateTagReply>(_testServerCallContext);

        _mediator.Send(Arg.Any<CanCreateTagQuery>()).Returns(mediatorResponses[0], mediatorResponses[1]);

        // Act
        using var call = _sut.CanCreateTag(requestStream, responseStream, _testServerCallContext);

        // Assert
        for (var i = 0; i < 3; i++)
        {
            requestStream.AddMessage(requests[i]);
            var reply = await responseStream.ReadNextAsync();
            reply.Should().BeEquivalentTo(expectedReplies[i]);
        }

        requestStream.Complete();
        await call;
        responseStream.Complete();

        var canCreateTagReply = await responseStream.ReadNextAsync();
        canCreateTagReply.Should().BeNull();
        await _mediator.Received(2).Send(Arg.Any<CanCreateTagQuery>());
    }
}
