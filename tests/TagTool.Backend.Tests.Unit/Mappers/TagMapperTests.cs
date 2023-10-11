using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using Xunit;

namespace TagTool.Backend.Tests.Unit.Mappers;

public class TagMapperTests
{
    private static readonly TypeRegistry? _typeRegistry = TypeRegistry.FromFiles(TypeTag.Descriptor.File);

    private readonly ITagMapper _sut = TagMapperHelper.InitializeWithKnownMappers();

    private static IMessage UnpackHelper(Any anyTag) => anyTag.Unpack(_typeRegistry);

    [Fact]
    public void MapFromDto_ValidTypeTag_ReturnsItemTypeTag()
    {
        // Arrange
        var type = typeof(TaggableFile);
        var tag = new TypeTag { Type = type.Name };

        // Act
        var tagBase = _sut.MapFromDto(Any.Pack(tag));

        // Assert
        tagBase.Should().BeOfType<ItemTypeTag>().Which.Type.Should().Be(type);
    }

    [Fact]
    public void MapFromDto_ValidTypeTag_ReturnsItemTypeTag2()
    {
        // Arrange
        var type = typeof(TaggableFolder);
        var tag = new TypeTag { Type = type.Name };

        // Act
        var tagBase = _sut.MapFromDto(Any.Pack(tag));

        // Assert
        tagBase.Should().BeOfType<ItemTypeTag>().Which.Type.Should().Be(type);
    }

    [Fact]
    public void MapToDto_ValidItemTypeTagWithTaggableFile_ReturnsTypeTag()
    {
        // Arrange
        var type = typeof(TaggableFile);
        var tag = new ItemTypeTag { Type = type };

        // Act
        var anyTag = _sut.MapToDto(tag);
        var tagDto = UnpackHelper(anyTag);

        // Assert
        tagDto.Should().BeOfType<TypeTag>().Which.Type.Should().Be(type.Name);
    }

    [Fact]
    public void MapToDto_ValidItemTypeTagWithTaggableFolder_ReturnsTypeTag()
    {
        // Arrange
        var type = typeof(TaggableFolder);
        var tag = new ItemTypeTag { Type = type };

        // Act
        var anyTag = _sut.MapToDto(tag);
        var tagDto = UnpackHelper(anyTag);

        // Assert
        tagDto.Should().BeOfType<TypeTag>().Which.Type.Should().Be(type.Name);
    }
}
