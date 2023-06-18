using System.Diagnostics;
using System.Globalization;

namespace TagTool.Backend.Models;

public interface ITagBase
{
}

[DebuggerDisplay("{FormattedName}")]
public abstract class TagBase : IHasTimestamps, ITagBase
{
    public int Id { get; set; }

    // The cleaner way to do this would be abstract get-only property...
    // however (sqlite's?) migration validation throws an error
    // 'No backing field could be found for property 'TagBase.FormattedName' and the property does not have a setter.'
    public string FormattedName { get; protected set; } = null!;

    public DateTime? Added { get; set; }

    public DateTime? Deleted { get; set; }

    public DateTime? Modified { get; set; }

    public ICollection<TaggedItem> TaggedItems { set; get; } = new List<TaggedItem>();
}

public sealed class NormalTag : TagBase
{
    private string _name = null!;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            FormattedName = nameof(NormalTag) + ":" + _name;
        }
    }
}

public sealed class ItemTypeTag : TagBase
{
    private string? _type;

    public string? Type
    {
        get => _type;
        set
        {
            _type = value;
            FormattedName = nameof(ItemTypeTag) + Type;
        }
    }
}

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

public sealed class SizeRangeTag : TagBase
{
    private double _min;

    public double Min
    {
        get => _min;
        set
        {
            _min = value;
            FormattedName = nameof(SizeRangeTag) + $":{value}-{Max}";
        }
    }

    public double Max { get; set; }
}

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
