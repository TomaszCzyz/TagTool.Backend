using JetBrains.Annotations;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public class ItemTypeTag : TagBase
{
    private string _typeName = null!;

    public Type Type
    {
        get => Type.GetType(_typeName)!;
        set
        {
            if (!value.IsAssignableTo(typeof(TaggableItem)))
            {
                throw new ArgumentException($"Only types derived from {nameof(TaggableItem)} can be use.", nameof(value));
            }

            _typeName = value.AssemblyQualifiedName!;
            FormattedName = nameof(ItemTypeTag) + ":" + value.Name;
        }
    }
}

[UsedImplicitly]
public class ItemTypeTagMapper : TagDtoMapper<ItemTypeTag, TypeTag>
{
    protected override ItemTypeTag MapFromDto(TypeTag dto)
    {
        return dto.Type switch
        {
            nameof(TaggableFile) => new ItemTypeTag { Type = typeof(TaggableFile) },
            nameof(TaggableFolder) => new ItemTypeTag { Type = typeof(TaggableFolder) },
            _ => throw new NotSupportedException($"TypeTag {dto} contains unknown type of taggable item")
        };
    }

    protected override TypeTag MapToDto(ItemTypeTag tag) => new() { Type = tag.Type.Name };
}
