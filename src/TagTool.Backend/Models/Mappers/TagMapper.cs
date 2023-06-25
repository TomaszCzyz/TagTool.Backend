using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace TagTool.Backend.Models.Mappers;

/// <summary>
///     Mapper for tag types between grpc contracts and domain types.
/// </summary>
/// <remarks>
///     In the future it should be extendable to any type of tag and registration should be automated.
///     It can be achieved by registering all tag types from assemblies (mark by something or in given namespace).
///     The registration should cache all type and not using reflection, for performance reasons.
/// </remarks>
public static class TagMapper
{
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
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };

        return Any.Pack(tagDto);
    }
}
