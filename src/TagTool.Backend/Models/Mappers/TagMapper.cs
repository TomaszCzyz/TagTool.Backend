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
        TagBase? tagBase = null;

        if (tag.Is(DomainTypes.NormalTag.Descriptor))
        {
            var normalTag = tag.Unpack<DomainTypes.NormalTag>();
            tagBase = new NormalTag { Name = normalTag.Name };
        }
        else if (tag.Is(DomainTypes.YearTag.Descriptor))
        {
            var yearTag = tag.Unpack<DomainTypes.YearTag>();
            tagBase = new YearTag { DateOnly = new DateOnly(yearTag.Year, 1, 1) };
        }
        else if (tag.Is(DomainTypes.DayTag.Descriptor))
        {
            var dayTag = tag.Unpack<DomainTypes.DayTag>();
            tagBase = new DayTag { DayOfWeek = (DayOfWeek)dayTag.Day };
        }
        else if (tag.Is(DomainTypes.DayRangeTag.Descriptor))
        {
            var dayRangeTag = tag.Unpack<DomainTypes.DayRangeTag>();
            tagBase = new DayRangeTag { Begin = (DayOfWeek)dayRangeTag.BeginDay, End = (DayOfWeek)dayRangeTag.EndDay };
        }
        else if (tag.Is(DomainTypes.MonthTag.Descriptor))
        {
            var monthTag = tag.Unpack<DomainTypes.MonthTag>();
            tagBase = new MonthTag { Month = monthTag.Month };
        }

        return tagBase ?? throw new ArgumentException("Unable to match tag type");
    }

    public static Any MapToDto(TagBase tag)
    {
        IMessage tagDto = tag switch
        {
            NormalTag normalTag => new DomainTypes.NormalTag { Name = normalTag.Name },
            YearTag yearTag => new DomainTypes.YearTag { Year = yearTag.DateOnly.Year },
            MonthTag monthTag => new DomainTypes.MonthTag { Month = monthTag.Month },
            DayTag dayTag => new DomainTypes.DayTag { Day = (int)dayTag.DayOfWeek },
            DayRangeTag dayRangeTag => new DomainTypes.DayRangeTag { BeginDay = (int)dayRangeTag.Begin, EndDay = (int)dayRangeTag.End },
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
        };

        return Any.Pack(tagDto);
    }
}
