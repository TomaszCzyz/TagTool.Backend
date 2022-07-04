namespace TagTool;

public class Tag
{
    public string Name { get; init; } = null!;

    public float HierarchyValue { get; set; }

    public IEnumerable<Tag>? ChildTags { get; set; }
}
