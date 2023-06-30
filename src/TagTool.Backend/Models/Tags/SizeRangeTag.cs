namespace TagTool.Backend.Models.Tags;

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
