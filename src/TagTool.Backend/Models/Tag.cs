namespace TagTool.Backend.Models;

public class Tag
{
    public int Id { get; set; }

    public required string Name { get; set; } = null!;

    public ICollection<TaggedItem> TaggedItems { get; set; } = new List<TaggedItem>();
}

public abstract class TagBase : IHasTimestamps
{
    public int Id { get; set; }

    public ICollection<TaggedItem> TaggedItems { get; set; } = new List<TaggedItem>();

    public DateTime? Added { get; set; }

    public DateTime? Deleted { get; set; }

    public DateTime? Modified { get; set; }
}

public class NormalTag : TagBase
{
    public string Name { get; set; } = null!;
}

public class ItemTypeTag : TagBase
{
    public string? Type { get; set; }
}

public class DateRangeTag : TagBase
{
    public DateTime Begin { get; set; }

    public DateTime End { get; set; }
}

public class SizeRangeTag : TagBase
{
    public double Min { get; set; }

    public double Max { get; set; }
}
