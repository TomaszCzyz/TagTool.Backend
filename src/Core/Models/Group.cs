namespace TagTool.Models;

public record Group
{
    public int Id { get; set; }

    public string Name { get; set; }

    public GroupType Type { get; set; }

    public List<Tag> Tags { get; set; }
}
