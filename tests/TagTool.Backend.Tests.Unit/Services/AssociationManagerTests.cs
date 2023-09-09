using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Services;

public class AssociationManagerTests : IDisposable
{
    private readonly Mock<ILogger<AssociationManager>> _loggerMock = new();
    private readonly TagToolDbContext _dbContextMock;

    public AssociationManagerTests()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TagToolDbContext>().UseInMemoryDatabase("TagToolDb").Options;

        _dbContextMock = new TagToolDbContext(optionsBuilder);

        var animalTag = new TextTag { Text = "Animal" };
        var animalBaseTag = new TextTag { Text = "AnimalBase" };
        var catTag = new TextTag { Text = "Cat" };
        var cat2Tag = new TextTag { Text = "Cat2" };
        var pussyTag = new TextTag { Text = "Pussy" };
        var dogTag = new TextTag { Text = "Dog" };

        _dbContextMock.Tags.AddRange(animalTag, animalBaseTag, catTag, cat2Tag, pussyTag, dogTag);
        _dbContextMock.SaveChanges();
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalTag, ChildGroups = new List<TagBase> { catTag } });
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalTag, ChildGroups = new List<TagBase> { dogTag } });
        // _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { ParentGroup = animalBaseTag, ChildGroups = new List<TagBase> { animalTag } });
        _dbContextMock.SaveChanges();
    }

    public void Dispose()
    {
        _dbContextMock.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddChild_ValidRequest_Correct()
    {
        // Arrange
        var associationManager = new AssociationManager(_dbContextMock);

        // Act
        _ = await associationManager.AddChild(new TextTag { Text = "Cat" }, new TextTag { Text = "Animal" }, CancellationToken.None);
        _ = await associationManager.AddChild(new TextTag { Text = "Pussy" }, new TextTag { Text = "Animal" }, CancellationToken.None);
        _ = await associationManager.AddChild(new TextTag { Text = "Dog" }, new TextTag { Text = "Animal" }, CancellationToken.None);
        _ = await associationManager.AddSynonym(new TextTag { Text = "Cat" }, "Cat Group", CancellationToken.None);
        _ = await associationManager.AddSynonym(new TextTag { Text = "Cat2" }, "Cat Group", CancellationToken.None);
        _ = await associationManager.AddSynonym(new TextTag { Text = "Pussy" }, "Cat Group", CancellationToken.None);
        var result = await associationManager.AddChild(new TextTag { Text = "Animal" }, new TextTag { Text = "AnimalBase" }, CancellationToken.None);

        // Assert
        result.Value.Should().BeOfType(typeof(None));
    }

    [Fact]
    public void Ctor_CorrectParameters_CorrectlyBuildTree()
    {
        // Arrange

        // Act
        var associationManager = new AssociationManager(_dbContextMock);

        // Assert
        // var mutableEntityTreeNode = associationManager.Root;
    }
}
