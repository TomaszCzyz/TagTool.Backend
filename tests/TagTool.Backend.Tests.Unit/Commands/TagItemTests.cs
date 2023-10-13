using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using NSubstitute;
using TagTool.Backend.Commands;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Commands;

public class TagItemTests
{
    private readonly TagItem _sut;

    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();

    private readonly TextTag _tag1 = new() { Text = "TestTag1" };
    private readonly TextTag _tag2 = new() { Text = "TestTag2" };
    private readonly TaggableFile _taggableFile = new() { Path = "path", Tags = new List<TagBase>() };

    public TagItemTests()
    {
        var logger = Substitute.For<ILogger<TagItem>>();
        var implicitTagsProvider = Substitute.For<IImplicitTagsProvider>();

        _sut = new TagItem(logger, implicitTagsProvider, _dbContext);
    }

    [Fact]
    private async Task Handle_ValidRequest_TagAndItemExists_ItemNotTaggedYet_ReturnsTaggableItem()
    {
        // Arrange
        var tags = new List<TagBase> { _tag1, _tag2 };
        var taggableFiles = new List<TaggableFile> { _taggableFile };

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var taggableFilesMock = taggableFiles.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TaggableFiles.Returns(taggableFilesMock);

        var command = new Backend.Commands.TagItemRequest { Tag = _tag1, TaggableItem = _taggableFile };

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().Be(_taggableFile)
            .And.Subject.As<TaggableFile>().Tags.Should().Contain(_tag1);
    }

    [Fact]
    private async Task Handle_ValidRequest_TagAndItemExists_ItemIsAlreadyTagged_ReturnsError()
    {
        // Arrange
        _taggableFile.Tags.Add(_tag1);
        var tags = new List<TagBase> { _tag1, _tag2 };
        var taggableFiles = new List<TaggableFile> { _taggableFile };

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var taggableFilesMock = taggableFiles.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TaggableFiles.Returns(taggableFilesMock);

        var command = new Backend.Commands.TagItemRequest { Tag = _tag1, TaggableItem = _taggableFile };

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().BeOfType<ErrorResponse>()
            .Which.Message.Should().Be($"Item {_taggableFile} already contain tag {_tag1}");
    }
}
