namespace TagTool.Backend.Models;

public class Tag
{
    public int Id { get; set; }

    public required string Name { get; set; } = null!;

    public ICollection<TaggedItem> TaggedItems { get; set; }  = new List<TaggedItem>();
}
