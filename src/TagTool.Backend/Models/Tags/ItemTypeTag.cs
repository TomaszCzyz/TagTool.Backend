using JetBrains.Annotations;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public abstract class ItemTypeTag : TagBase
{
}

public sealed class FileTypeTag : ItemTypeTag
{
    public FileTypeTag()
    {
        FormattedName = "ItemTypeTag:" + nameof(TaggableFile);
    }
}

public sealed class FolderTypeTag : ItemTypeTag
{
    public FolderTypeTag()
    {
        FormattedName = "ItemTypeTag:" + nameof(TaggableFolder);
    }
}

[UsedImplicitly]
public class ItemTypeTagMapper : TagDtoMapper<ItemTypeTag, TypeTag>
{
    protected override ItemTypeTag MapFromDto(TypeTag dto)
        => dto.Type switch
        {
            nameof(TaggableFile) => new FileTypeTag(),
            nameof(TaggableFolder) => new FolderTypeTag(),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

    protected override TypeTag MapToDto(ItemTypeTag tag)
        => tag switch
        {
            FileTypeTag => new TypeTag { Type = nameof(TaggableFile) },
            FolderTypeTag => new TypeTag { Type = nameof(TaggableFolder) },
            _ => throw new ArgumentOutOfRangeException(nameof(tag))
        };
}
