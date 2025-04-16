using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Services;

public class TaggableItemMapper
{
    private readonly Dictionary<string, ITaggableItemMapper> _stringTypeToMapper;
    private readonly Dictionary<Type, ITaggableItemMapper> _typeToMapper;

    public TaggableItemMapper(IEnumerable<ITaggableItemMapper> mappers)
    {
        var taggableItemMappers = mappers as ITaggableItemMapper[] ?? mappers.ToArray();
        _stringTypeToMapper = taggableItemMappers.ToDictionary(mapper => mapper.ItemType, StringComparer.OrdinalIgnoreCase);
        _typeToMapper = taggableItemMappers.ToDictionary(mapper => mapper.SelfType);
    }

    public TaggableItem MapFromString(string type, string payload)
    {
        if (_stringTypeToMapper.TryGetValue(type, out var mapper))
        {
            return mapper.MapFromString(payload);
        }

        throw new ArgumentOutOfRangeException(nameof(type), type, $"There is no mapper for type {type}.");
    }

    public (string Type, string Payload) MapToString(TaggableItem item)
    {
        if (_typeToMapper.TryGetValue(item.GetType(), out var mapper))
        {
            return mapper.MapToString(item);
        }

        throw new ArgumentOutOfRangeException(nameof(item), item, $"There is no mapper for type {item.GetType().FullName}.");
    }
}
