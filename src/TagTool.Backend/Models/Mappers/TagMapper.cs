using System.Collections.Concurrent;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.DomainTypes;
using Type = System.Type;

namespace TagTool.Backend.Models.Mappers;

/// <summary>
///     Mapper for tag types between grpc contracts and domain types.
/// </summary>
/// <remarks>
///     In the future it should be extendable to any type of tag and registration should be automated.
///     It can be achieved by registering all tag types from assemblies (mark by something or in given namespace).
///     The registration should cache all type and not using reflection, for performance reasons.
/// </remarks>
public class TagMapper
{
    private static readonly TypeRegistry? _typeRegistry = TypeRegistry.FromFiles(TypeTag.Descriptor.File);

    private readonly ConcurrentDictionary<Type, ITagFromDtoMapper> _fromDtoMappers = new();
    private readonly ConcurrentDictionary<Type, ITagToDtoMapper> _toDtoMappers = new();

    // ReSharper disable once ParameterTypeCanBeEnumerable.Local
    // Reason: mappers collection is registered as IReadOnlyCollection and it has to be retrieved as such.
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

        return _fromDtoMappers[tagDto.GetType()].FromDto(tagDto);
    }

    public Any MapToDto2(TagBase tag)
    {
        var message = _toDtoMappers[tag.GetType()].ToDto(tag);

        return Any.Pack(message);
    }

    public static TagBase MapToDomain(Any tag)
    {
        if (tag.Is(DomainTypes.NormalTag.Descriptor))
        {
            var normalTag = tag.Unpack<DomainTypes.NormalTag>();
            return new NormalTag { Name = normalTag.Name };
        }

        if (tag.Is(DomainTypes.YearTag.Descriptor))
        {
            var yearTag = tag.Unpack<DomainTypes.YearTag>();
            return new YearTag { DateOnly = new DateOnly(yearTag.Year, 1, 1) };
        }

        if (tag.Is(DomainTypes.DayTag.Descriptor))
        {
            var dayTag = tag.Unpack<DomainTypes.DayTag>();
            return new DayTag { DayOfWeek = (DayOfWeek)dayTag.Day };
        }

        if (tag.Is(DomainTypes.DayRangeTag.Descriptor))
        {
            var dayRangeTag = tag.Unpack<DomainTypes.DayRangeTag>();
            return new DayRangeTag { Begin = (DayOfWeek)dayRangeTag.Begin, End = (DayOfWeek)dayRangeTag.End };
        }

        if (tag.Is(DomainTypes.MonthTag.Descriptor))
        {
            var monthTag = tag.Unpack<DomainTypes.MonthTag>();
            return new MonthTag { Month = monthTag.Month };
        }

        if (tag.Is(DomainTypes.MonthRangeTag.Descriptor))
        {
            var monthTag = tag.Unpack<DomainTypes.MonthRangeTag>();
            return new MonthRangeTag { Begin = monthTag.Begin, End = monthTag.End };
        }

        if (tag.Is(TypeTag.Descriptor))
        {
            var typeTag = tag.Unpack<TypeTag>();
            TagBase tagBase = typeTag.Type switch
            {
                nameof(TaggableFile) => new FileTypeTag(),
                nameof(TaggableFolder) => new FolderTypeTag(),
                _ => throw new UnreachableException()
            };
            return tagBase;
        }

        throw new ArgumentException("Unable to match tag type");
    }

    public static Any MapToDto(TagBase tag)
    {
        IMessage tagDto = tag switch
        {
            NormalTag normalTag => new DomainTypes.NormalTag { Name = normalTag.Name },
            YearTag yearTag => new DomainTypes.YearTag { Year = yearTag.DateOnly.Year },
            MonthTag monthTag => new DomainTypes.MonthTag { Month = monthTag.Month },
            MonthRangeTag monthRangeTag => new DomainTypes.MonthRangeTag { Begin = monthRangeTag.Begin, End = monthRangeTag.End },
            DayTag dayTag => new DomainTypes.DayTag { Day = (int)dayTag.DayOfWeek },
            DayRangeTag dayRangeTag => new DomainTypes.DayRangeTag { Begin = (int)dayRangeTag.Begin, End = (int)dayRangeTag.End },
            FileTypeTag => new TypeTag { Type = nameof(TaggableFile) },
            FolderTypeTag => new TypeTag { Type = nameof(TaggableFolder) },
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };

        return Any.Pack(tagDto);
    }
}
