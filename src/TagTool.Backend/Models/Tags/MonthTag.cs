using System.Globalization;
using JetBrains.Annotations;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public sealed class MonthTag : TagBase
{
    private int _month;

    public int Month
    {
        get => _month;
        set
        {
            _month = value;
            FormattedName = nameof(MonthTag) + ":" + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(value);
        }
    }
}

[UsedImplicitly]
public class MonthRangeTagMapper : TagDtoMapper<MonthRangeTag, DomainTypes.MonthRangeTag>
{
    protected override MonthRangeTag MapFromDto(DomainTypes.MonthRangeTag dto) => new() { Begin = dto.Begin, End = dto.Begin };

    protected override DomainTypes.MonthRangeTag MapToDto(MonthRangeTag tag) => new() { Begin = tag.Begin, End = tag.End };
}
