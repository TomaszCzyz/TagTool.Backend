namespace TagTool.Backend.Models.Tags;

public sealed class DateRangeTag : TagBase
{
    private DateTime _begin;
    private DateTime _end;

    public DateTime Begin
    {
        get => _begin;
        set
        {
            _begin = value;
            FormattedName = nameof(DateRangeTag) + $":{value}-{End}";
        }
    }

    public DateTime End
    {
        get => _end;
        set
        {
            _end = value;
            FormattedName = nameof(DateRangeTag) + $":{Begin}-{value}";
        }
    }
}
