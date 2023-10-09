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
    // private readonly DbSet<TagBase> _tags = Substitute.For<DbSet<TagBase>, IQueryable<TagBase>, IAsyncEnumerable<TagBase>>();

    public TagItemTests()
    {
        // var tagToolDbContext = new TagTool.Backend.DbContext.TagToolDbContext();
        var logger = Substitute.For<ILogger<TagItem>>();
        var implicitTagsProvider = Substitute.For<IImplicitTagsProvider>();

        _sut = new TagItem(logger, implicitTagsProvider, _dbContext);
    }

    [Fact]
    private async Task Handle_ValidRequest_TagAndItemExists_ItemNotTaggedYet_ReturnTaggableItem()
    {
        // Arrange
        var testTag1 = new TextTag { Text = "TestTag1" };
        var testTag2 = new TextTag { Text = "TestTag2" };
        var tags = new List<TagBase> { testTag1, testTag2 };
        var taggableItem = new TaggableFile { Path = "path", Tags = new List<TagBase>() };
        var taggableFiles = new List<TaggableFile> { taggableItem }.AsQueryable();

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var taggableFilesMock = taggableFiles.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TaggableFiles.Returns(taggableFilesMock);

        var command = new Backend.Commands.TagItemRequest { Tag = testTag1, TaggableItem = taggableItem };

        // Act
        var response = await _sut.Handle(command, CancellationToken.None);

        // Assert
        response.Value.Should().Be(taggableItem)
            .And.As<TaggableFile>().Tags.Should().Contain(testTag1);
    }
}
