using System.Globalization;
using JetBrains.Annotations;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public sealed class DayTag : TagBase
{
    private DayOfWeek _dayOfWeek;

    public DayOfWeek DayOfWeek
    {
        get => _dayOfWeek;
        set
        {
            FormattedName = nameof(DayTag) + ":" + CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(value);
            _dayOfWeek = value;
        }
    }
}

[UsedImplicitly]
public class DayTagMapper : TagDtoMapper<DayTag, DomainTypes.DayTag>
{
    protected override DayTag MapFromDto(DomainTypes.DayTag dto) => new() { DayOfWeek = (DayOfWeek)dto.Day };

    protected override DomainTypes.DayTag MapToDto(DayTag tag) => new() { Day = (int)tag.DayOfWeek };
}
