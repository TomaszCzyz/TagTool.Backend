using FluentAssertions;
using NSubstitute;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task SearchTags_ValidRequest_ReturnsTags()
    {
        // Arrange
        var (tag1, tag2, tag3) = (new TextTag { Text = "Test1" }, new TextTag { Text = "Test2" }, new TextTag { Text = "Test3" });
        var mediatorResponses = new List<(TagBase, IEnumerable<TextSlice>)>
        {
            (tag1, new TextSlice[] { new(0, 999) }),
            (tag2, new TextSlice[] { new(0, 10), new(5, 10) }),
            (tag3, new TextSlice[] { new(0, 999) })
        };
        var replies = new List<SearchTagsReply>
        {
            new()
            {
                Tag = _tagMapper.MapToDto(tag1),
                MatchedPart = { new[] { new SearchTagsReply.Types.MatchedPart { StartIndex = 0, Length = 999 } } }
            },
            new()
            {
                Tag = _tagMapper.MapToDto(tag2),
                MatchedPart =
                {
                    new[]
                    {
                        new SearchTagsReply.Types.MatchedPart { StartIndex = 0, Length = 10 },
                        new SearchTagsReply.Types.MatchedPart { StartIndex = 5, Length = 10 }
                    }
                }
            },
            new()
            {
                Tag = _tagMapper.MapToDto(tag3),
                MatchedPart = { new[] { new SearchTagsReply.Types.MatchedPart { StartIndex = 0, Length = 999 } } }
            }
        };
        var request = new SearchTagsRequest
        {
            SearchText = "TestSearchText",
            SearchType = SearchTagsRequest.Types.SearchType.Fuzzy,
            ResultsLimit = 10
        };
        var cts = new CancellationTokenSource();
        var testServerCallContext = TestServerCallContext.Create(cancellationToken: cts.Token);
        var responseStream = new TestServerStreamWriter<SearchTagsReply>(testServerCallContext);

        _mediator.CreateStream(Arg.Any<SearchTagsFuzzyRequest>(), Arg.Any<CancellationToken>()).Returns(mediatorResponses.ToAsyncEnumerable());

        // Act
        using var call = _sut.SearchTags(request, responseStream, _testServerCallContext);

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

        _ = _mediator.Received(1).CreateStream(Arg.Any<SearchTagsFuzzyRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SearchTags_InvalidRequest_DefaultsToStartsWithSearch_NotThrows()
    {
        // Arrange
        var request = new SearchTagsRequest { SearchText = "TestSearchText", ResultsLimit = 10 };

        var responseStream = new TestServerStreamWriter<SearchTagsReply>(_testServerCallContext);

        // Act
        using var call = _sut.SearchTags(request, responseStream, _testServerCallContext);
        var act = () => responseStream.ReadAllAsync();
        responseStream.Complete();

        // Assert
        act.Should().NotThrow<ArgumentOutOfRangeException>();
        _ = _mediator.Received(1).CreateStream(Arg.Any<SearchTagsStartsWithRequest>(), Arg.Any<CancellationToken>());
    }
}
