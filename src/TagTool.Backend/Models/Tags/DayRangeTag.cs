using System.Globalization;
using JetBrains.Annotations;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public sealed class DayRangeTag : TagBase
{
    private DayOfWeek _begin;
    private DayOfWeek _end;

    public DayOfWeek Begin
    {
        get => _begin;
        set
        {
            _begin = value;
            FormattedName = nameof(DayRangeTag)
                            + $":{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(value)}"
                            + $"-{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(End)}";
        }
    }

    public DayOfWeek End
    {
        get => _end;
        set
        {
            _end = value;
            FormattedName = nameof(DayRangeTag)
                            + $":{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(Begin)}"
                            + $"-{CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(value)}";
        }
    }
}

[UsedImplicitly]
public class DayRangeTagMapper : TagDtoMapper<DayRangeTag, DomainTypes.DayRangeTag>
{
    protected override DayRangeTag MapFromDto(DomainTypes.DayRangeTag dto) => new() { Begin = (DayOfWeek)dto.Begin, End = (DayOfWeek)dto.End };

    protected override DomainTypes.DayRangeTag MapToDto(DayRangeTag tag) => new() { Begin = (int)tag.Begin, End = (int)tag.End };
}
