namespace TagTool.Backend.Models;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public float HierarchyValue { get; set; }

    public List<Group> Groups { get; set; } = null!;

    public ICollection<File> Files { get; } = new List<File>();
}
