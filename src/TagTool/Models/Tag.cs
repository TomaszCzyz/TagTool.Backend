namespace TagTool.Models;

public record Tag
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public float HierarchyValue { get; init; }

    public IList<Group>? Groups { get; init; }

    public IList<File>? Files { get; init; }
}
