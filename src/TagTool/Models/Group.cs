namespace TagTool.Models;

public record Group
{
    public int Id { get; init; }

    public string Name { get; init; }

    public GroupType Type { get; init; }

    public IList<Tag> Tags { get; init; }
}
