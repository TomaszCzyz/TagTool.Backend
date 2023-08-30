using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
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
        var pussyTag = new TextTag { Text = "Pussy" };
        var dogTag = new TextTag { Text = "Dog" };

        _dbContextMock.Tags.AddRange(animalTag, animalBaseTag, catTag, pussyTag, dogTag);
        _dbContextMock.SaveChanges();
        _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { BaseTag = animalTag, ChildTags = new List<TagBase> { catTag } });
        _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { BaseTag = animalTag, ChildTags = new List<TagBase> { dogTag } });
        _dbContextMock.TagsHierarchy.Add(new TagsHierarchy { BaseTag = animalBaseTag, ChildTags = new List<TagBase> { animalTag } });
        _dbContextMock.SaveChanges();
    }

    [Fact]
    public void Ctor_CorrectParameters_CorrectlyBuildTree()
    {
        // Arrange

        // Act
        var associationManager = new AssociationManager(_loggerMock.Object, _dbContextMock);

        // Assert
        var mutableEntityTreeNode = associationManager.Root;
    }

    public void Dispose()
    {
        _dbContextMock.Dispose();
        GC.SuppressFinalize(this);
    }
}
