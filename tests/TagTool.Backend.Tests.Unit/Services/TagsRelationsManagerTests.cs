using FluentAssertions;
using MockQueryable.NSubstitute;
using NSubstitute;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class TagsRelationsManagerTests
{
    private readonly TagsRelationsManager _sut;
    private readonly ITagToolDbContext _dbContext = Substitute.For<ITagToolDbContext>();

    private readonly TextTag _animalTag = new() { Text = "Animal" };
    private readonly TextTag _animalBaseTag = new() { Text = "AnimalBase" };
    private readonly TextTag _catTag = new() { Text = "Cat" };
    private readonly TextTag _cat2Tag = new() { Text = "Cat2" };
    private readonly TextTag _pussyTag = new() { Text = "Pussy" };
    private readonly TextTag _dogTag = new() { Text = "Dog" };

    private readonly TagSynonymsGroup _synonymsGroup1 = new()
    {
        Id = 0,
        Name = "TestGroup1",
        Synonyms = new List<TagBase>()
    };

    private readonly TagSynonymsGroup _synonymsGroup2 = new()
    {
        Id = 1,
        Name = "TestGroup2",
        Synonyms = new List<TagBase>()
    };

    public TagsRelationsManagerTests()
    {
        var tags = new List<TagBase>
        {
            _animalTag,
            _animalBaseTag,
            _catTag,
            _cat2Tag,
            _pussyTag,
            _dogTag
        };

        var synonymsGroups = new List<TagSynonymsGroup> { _synonymsGroup1, _synonymsGroup2 };

        var tagsMock = tags.AsQueryable().BuildMockDbSet();
        var synonymsGroupsMock = synonymsGroups.AsQueryable().BuildMockDbSet();

        _dbContext.Tags.Returns(tagsMock);
        _dbContext.TagSynonymsGroups.Returns(synonymsGroupsMock);

        _sut = new TagsRelationsManager(_dbContext);
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalTag, ChildGroups = new List<TagBase> { catTag } });
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalTag, ChildGroups = new List<TagBase> { dogTag } });
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalBaseTag, ChildGroups = new List<TagBase> { animalTag } });
    }

    [Fact]
    public async Task AddSynonym_ExistingGroupWhichDoesNotContainRequestedTag_AddsTagToSynonymsGroup()
    {
        // Arrange
        _synonymsGroup1.Synonyms.Add(_catTag);
        _synonymsGroup1.Synonyms.Add(_cat2Tag);

        // Act
        var addSynonym = await _sut.AddSynonym(_pussyTag, _synonymsGroup1.Name, CancellationToken.None);

        // Assert
        addSynonym.Value.Should().BeOfType<None>();
        _synonymsGroup1.Synonyms.Should().Contain(_pussyTag);
    }

    [Fact]
    public async Task AddSynonym_GroupExistsAndContainsRequestedTag_ReturnsError()
    {
        // Arrange
        _synonymsGroup1.Synonyms.Add(_catTag);
        _synonymsGroup1.Synonyms.Add(_cat2Tag);
        _synonymsGroup1.Synonyms.Add(_pussyTag);

        // Act
        var addSynonym = await _sut.AddSynonym(_pussyTag, _synonymsGroup1.Name, CancellationToken.None);

        // Assert
        addSynonym.Value.Should().BeOfType<ErrorResponse>()
            .Which.Message.Should().Be($"The tag {_pussyTag} is already in a requested group {_synonymsGroup1}.");
    }

    [Fact]
    public async Task AddSynonym_GroupExists_ButTagIsAlreadyInDifferentGroup_ReturnsError()
    {
        // Arrange
        _synonymsGroup1.Synonyms.Add(_catTag);
        _synonymsGroup1.Synonyms.Add(_cat2Tag);
        _synonymsGroup2.Synonyms.Add(_pussyTag);

        // Act
        var addSynonym = await _sut.AddSynonym(_pussyTag, _synonymsGroup1.Name, CancellationToken.None);

        // Assert
        addSynonym.Value.Should().BeOfType<ErrorResponse>()
            .Which.Message.Should().Be($"The tag {_pussyTag} is already in different synonyms group {_synonymsGroup2}");
    }

    [Fact]
    public async Task AddChild_ValidRequest_Correct()
    {
        // Arrange

        // Act
        var act = async () =>
        {
            _ = await _sut.AddChild(new TextTag { Text = "Cat" }, new TextTag { Text = "Animal" }, CancellationToken.None);
            _ = await _sut.AddChild(new TextTag { Text = "Pussy" }, new TextTag { Text = "Animal" }, CancellationToken.None);
            _ = await _sut.AddChild(new TextTag { Text = "Dog" }, new TextTag { Text = "Animal" }, CancellationToken.None);
            _ = await _sut.AddSynonym(new TextTag { Text = "Cat" }, "Cat Group", CancellationToken.None);
            _ = await _sut.AddSynonym(new TextTag { Text = "Cat2" }, "Cat Group", CancellationToken.None);
            _ = await _sut.AddSynonym(new TextTag { Text = "Pussy" }, "Cat Group", CancellationToken.None);
            _ = await _sut.AddChild(new TextTag { Text = "Animal" }, new TextTag { Text = "AnimalBase" }, CancellationToken.None);
        };

        // Assert
        await act.Should().NotThrowAsync();
        // todo: check if correct relations were added 
    }
}
