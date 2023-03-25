namespace TagTool.Backend.Models;

public class TaggedItem
{
    public int Id { get; set; }

    public required string ItemType { get; set; }

    public required string UniqueIdentifier { get; set; }

    public ICollection<Tag> Tags { get; init; } = new List<Tag>();
}
