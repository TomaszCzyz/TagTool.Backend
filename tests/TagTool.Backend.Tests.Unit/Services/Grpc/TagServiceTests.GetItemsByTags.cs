using FluentAssertions;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using NSubstitute;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using TagTool.Backend.Queries;
using Xunit;
using YearTagDto = TagTool.Backend.DomainTypes.YearTag;

namespace TagTool.Backend.Tests.Unit.Services.Grpc;

public partial class TagServiceTests
{
    [Fact]
    public async Task GetItemsByTags_ValidRequest_ReturnsTaggedItems()
    {
        // Arrange
        var taggableFileDto = new TaggableItemDto { File = _fileDto };
        var taggableFolderDto = new TaggableItemDto { Folder = _folderDto };
        var taggableItems = new List<TaggableItem>
        {
            _taggableItemMapper.MapFromDto(taggableFileDto), _taggableItemMapper.MapFromDto(taggableFolderDto)
        };

        var tagQueryParams = new RepeatedField<TagQueryParam>
        {
            new TagQueryParam { Tag = Any.Pack(_textTag) },
            new TagQueryParam { Tag = Any.Pack(_dayTag), State = TagQueryParam.Types.QuerySegmentState.Exclude }
        };

        var request = new GetItemsByTagsRequest { QueryParams = { tagQueryParams } };

        _mediator.Send(Arg.Any<GetItemsByTagsQuery>()).Returns(_ => taggableItems);

        // Act
        var response = await _sut.GetItemsByTags(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetItemsByTagsQuery>());
        response.TaggedItems.Should().HaveCount(2);
        response.TaggedItems.Select(item => item.TaggableItem).Should().BeEquivalentTo(new[] { taggableFileDto, taggableFolderDto });
    }

    [Fact]
    public async Task GetItemsByTags_ValidRequest_NoItemsFound_ReturnsEmpty()
    {
        // Arrange
        var tagQueryParams = new RepeatedField<TagQueryParam>
        {
            new TagQueryParam { Tag = Any.Pack(_textTag) },
            new TagQueryParam { Tag = Any.Pack(_dayTag), State = TagQueryParam.Types.QuerySegmentState.Exclude }
        };

        var request = new GetItemsByTagsRequest { QueryParams = { tagQueryParams } };

        _mediator.Send(Arg.Any<GetItemsByTagsQuery>()).Returns(_ => new List<TaggableItem>());

        // Act
        var response = await _sut.GetItemsByTags(request, _testServerCallContext);

        // Assert
        await _mediator.Received(1).Send(Arg.Any<GetItemsByTagsQuery>());
        response.TaggedItems.Should().NotBeNull();
        response.TaggedItems.Should().HaveCount(0);
    }

    [Fact]
    public async Task GetItemsByTags_ValidRequest_TagQueryIsEmpty_DoesNotThrows()
    {
        // Arrange
        var request = new GetItemsByTagsRequest();

        // Act
        var act = () => _sut.GetItemsByTags(request, _testServerCallContext);

        // Assert
        await act.Should().NotThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetItemsByTags_InvalidRequest_TagInQueryIsNull_Throws()
    {
        // Arrange
        var tagQueryParams = new RepeatedField<TagQueryParam>
        {
            new TagQueryParam { Tag = Any.Pack(_textTag) }, new TagQueryParam { State = TagQueryParam.Types.QuerySegmentState.Exclude }
        };

        var request = new GetItemsByTagsRequest { QueryParams = { tagQueryParams } };

        // Act
        var act = () => _sut.GetItemsByTags(request, _testServerCallContext);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("No tag in a tag query can be null*");
    }
}
