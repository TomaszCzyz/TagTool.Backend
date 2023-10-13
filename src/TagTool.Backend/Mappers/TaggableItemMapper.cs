using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;

namespace TagTool.Backend.Mappers;

public interface ITaggableItemMapper
{
    TaggableItem MapFromDto(TaggableItemDto dtoVariant);

    TaggableItemDto MapToDto(TaggableItem taggableItem);
}

public class TaggableItemMapper : ITaggableItemMapper
{
    public TaggableItem MapFromDto(TaggableItemDto dtoVariant)
    {
        return dtoVariant.ItemCase switch
        {
            TaggableItemDto.ItemOneofCase.File => new TaggableFile { Path = dtoVariant.File.Path },
            TaggableItemDto.ItemOneofCase.Folder => new TaggableFolder { Path = dtoVariant.Folder.Path },
            TaggableItemDto.ItemOneofCase.None => throw new ArgumentNullException(nameof(dtoVariant)),
            _ => throw new ArgumentOutOfRangeException(nameof(dtoVariant), "Provided item case is not supported.")
        };
    }

    public TaggableItemDto MapToDto(TaggableItem taggableItem)
    {
        return taggableItem switch
        {
            TaggableFile file => new TaggableItemDto { File = new FileDto { Path = file.Path } },
            TaggableFolder folder => new TaggableItemDto { Folder = new FolderDto { Path = folder.Path } },
            _ => throw new ArgumentOutOfRangeException(nameof(taggableItem), "Provided taggable item is not supported.")
        };
    }
}
