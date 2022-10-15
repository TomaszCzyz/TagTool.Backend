namespace TagTool.Backend.Models;

public class Tag
{
    public string Name { get; set; } = null!;

    public float HierarchyValue { get; set; }

    public List<Group> Groups { get; set; } = null!;
}
