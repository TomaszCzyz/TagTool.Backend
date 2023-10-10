using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using TagTool.Backend.Commands;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Commands;

public class UntagItemTests
{
    private readonly UntagItem _sut;
    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();

    private readonly TextTag _tag1 = new() { Text = "TestTag1" };
    private readonly TextTag _tag2 = new() { Text = "TestTag2" };
    private readonly TaggableFile _taggableFile = new() { Path = "path", Tags = new List<TagBase>() };

    public UntagItemTests()
    {
        var logger = Substitute.For<ILogger<UntagItem>>();

        _sut = new UntagItem(logger, _dbContext);
    }

    [Fact]
    private async Task Handle_ValidRequest_TagAndItemExists_ItemIsTagged_ReturnsTaggableItem()
    {
        // Arrange
        _taggableFile.Tags.Add(_tag1);
        var tags = new List<TagBase> { _tag1, _tag2 };
        var taggableFiles = new List<TaggableFile> { _taggableFile };

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var taggableFilesMock = taggableFiles.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TaggableFiles.Returns(taggableFilesMock);

        var command = new Backend.Commands.UntagItemRequest { Tag = _tag1, TaggableItem = _taggableFile };

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().Be(_taggableFile)
            .And.Subject.As<TaggableFile>().Tags.Should().NotContain(_tag1);
    }

    [Fact]
    private async Task Handle_ValidRequest_TagAndItemExists_ItemIsNotTagged_ReturnsError()
    {
        // Arrange
        var tags = new List<TagBase> { _tag1, _tag2 };
        var taggableFiles = new List<TaggableFile> { _taggableFile };

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var taggableFilesMock = taggableFiles.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TaggableFiles.Returns(taggableFilesMock);

        var command = new Backend.Commands.UntagItemRequest { Tag = _tag1, TaggableItem = _taggableFile };

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().BeOfType<ErrorResponse>()
            .Which.Message.Should().Be($"Unable to remove tag {_tag1} from item {_taggableFile}, item might not be tagged with given tag.");
    }
}
