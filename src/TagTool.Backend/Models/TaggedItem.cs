using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public abstract class TaggableItem
{
    public Guid Id { get; set; }

    public List<TagBase> Tags { get; set; } = new();
}

public class TaggableFile : TaggableItem
{
    public required string Path { get; set; }
}

public class TaggableFolder : TaggableItem
{
    public required string Path { get; set; }
}
