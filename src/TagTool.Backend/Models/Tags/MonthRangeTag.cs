using System.Globalization;
using JetBrains.Annotations;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public sealed class MonthRangeTag : TagBase
{
    private int _begin = 1;
    private int _end = 13;

    public int Begin
    {
        get => _begin;
        set
        {
            _begin = value;
            FormattedName = nameof(MonthRangeTag)
                            + $":{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(value)}"
                            + $"-{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(End)}";
        }
    }

    public int End
    {
        get => _end;
        set
        {
            _end = value;
            FormattedName = nameof(MonthRangeTag)
                            + $":{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Begin)}"
                            + $"-{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(value)}";
        }
    }
}

[UsedImplicitly]
public class MonthTagMapper : TagDtoMapper<MonthTag, DomainTypes.MonthTag>
{
    protected override MonthTag MapFromDto(DomainTypes.MonthTag dto) => new() { Month = dto.Month };

    protected override DomainTypes.MonthTag MapToDto(MonthTag tag) => new() { Month = tag.Month };
}
