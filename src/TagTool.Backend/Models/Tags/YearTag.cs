namespace TagTool.Backend.Models.Tags;

public sealed class YearTag : TagBase
{
    private DateOnly _dateOnly;

    public DateOnly DateOnly
    {
        get => _dateOnly;
        set
        {
            _dateOnly = value;
            FormattedName = nameof(YearTag) + ":" + value;
        }
    }
}
