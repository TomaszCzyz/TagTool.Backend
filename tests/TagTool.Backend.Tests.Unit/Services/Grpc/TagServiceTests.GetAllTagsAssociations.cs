using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task GetAllTagsAssociations_ValidRequestWithTag_ReturnsCorrectReplies()
    {
        // Arrange
        var groupName1 = "groupName1";
        var groupName2 = "groupName2";
        var request = new GetAllTagsAssociationsRequest { Tag = Any.Pack(_textTag) };

        var mediatorResponses = new List<ITagsRelationsManager.GroupDescription>
        {
            new(groupName1, new List<TagBase>(), new List<string>()), new(groupName2, new List<TagBase>(), new List<string>())
        };

        var replies = new List<GetAllTagsAssociationsReply> { new() { GroupName = groupName1 }, new() { GroupName = groupName2 } };

        var cts = new CancellationTokenSource();
        var testServerCallContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<GetAllTagsAssociationsReply>(testServerCallContext);

        _mediator.CreateStream(Arg.Any<GetAllTagsAssociationsQuery>(), Arg.Any<CancellationToken>()).Returns(mediatorResponses.ToAsyncEnumerable());

        // Act
        using var call = _sut.GetAllTagsAssociations(request, responseStream, _testServerCallContext);

        // Assert
        cts.Cancel();
        await call;
        responseStream.Complete();

        using var it = replies.GetEnumerator();
        // ReSharper disable once UseCancellationTokenForIAsyncEnumerable, we would get OperationCancelled exception,
        // Normally ReadAllAsync() would be called by the client (different thread),
        // but test method is executed by single thread. 
        await foreach (var reply in responseStream.ReadAllAsync())
        {
            it.MoveNext();
            reply.Should().BeEquivalentTo(it.Current);
        }

        var canCreateTagReply = await responseStream.ReadNextAsync();
        canCreateTagReply.Should().BeNull();
        _ = _mediator.Received(1).CreateStream(Arg.Any<GetAllTagsAssociationsQuery>(), Arg.Any<CancellationToken>());
    }
}
