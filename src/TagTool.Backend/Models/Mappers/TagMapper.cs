using System.Collections.Concurrent;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.DomainTypes;
using Type = System.Type;

namespace TagTool.Backend.Models.Mappers;

public interface ITagMapper
{
    TagBase MapFromDto(Any anyTag);

    Any MapToDto(TagBase tag);
}

/// <summary>
///     Mapper for tag types between grpc contracts and domain types.
/// </summary>
/// <remarks>
///     In the future it should be extendable to any type of tag and registration should be automated.
///     It can be achieved by registering all tag types from assemblies (mark by something or in given namespace).
///     The registration should cache all type and not using reflection, for performance reasons.
/// </remarks>
public class TagMapper : ITagMapper
{
    private static readonly TypeRegistry? _typeRegistry = TypeRegistry.FromFiles(TypeTag.Descriptor.File);

    private readonly ConcurrentDictionary<Type, ITagFromDtoMapper> _fromDtoMappers = new();
    private readonly ConcurrentDictionary<Type, ITagToDtoMapper> _toDtoMappers = new();

    public TagMapper(IReadOnlyCollection<ITagFromDtoMapper> tagFromDtoMappers, IReadOnlyCollection<ITagToDtoMapper> tagToDtoMappers)
    {
        foreach (var tagFromDtoMapper in tagFromDtoMappers)
        {
            var mapperBaseType = tagFromDtoMapper.GetType().BaseType;
            if (mapperBaseType?.GenericTypeArguments is not [_, var dtoType])
            {
                throw new NotSupportedException($"Only mappers derived from {typeof(TagDtoMapper<,>).Name} are supported");
            }

            _fromDtoMappers.AddOrUpdate(dtoType, _ => tagFromDtoMapper, (_, _) => tagFromDtoMapper);
        }

        foreach (var tagToDtoMapper in tagToDtoMappers)
        {
            var mapperBaseType = tagToDtoMapper.GetType().BaseType;
            if (mapperBaseType?.GenericTypeArguments is not [var tagType, _])
            {
                throw new NotSupportedException($"Only mappers derived from {typeof(TagDtoMapper<,>).Name} are supported");
            }

            _toDtoMappers.AddOrUpdate(tagType, _ => tagToDtoMapper, (_, _) => tagToDtoMapper);
        }
    }

    public TagBase MapFromDto(Any anyTag)
    {
        var tagDto = anyTag.Unpack(_typeRegistry) ?? throw new ArgumentException("unable to unpack tag", nameof(anyTag));

        if (!_fromDtoMappers.TryGetValue(tagDto.GetType(), out var mapper))
        {
            throw new NotSupportedException($"There is no mapper for type {tagDto.GetType()}");
        }

        return mapper.FromDto(tagDto);
    }

    public Any MapToDto(TagBase tag)
    {
        if (!_toDtoMappers.TryGetValue(tag.GetType(), out var mapper))
        {
            throw new NotSupportedException($"There is no mapper for type {tag.GetType()}");
        }

        return Any.Pack(mapper.ToDto(tag));
    }
}
