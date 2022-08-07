namespace TagTool.Backend.Models;

public record Group
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public GroupType Type { get; set; }

    public List<Tag> Tags { get; set; } = null!;
}
