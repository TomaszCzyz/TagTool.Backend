using System.Collections;

namespace TagTool.Backend.Models;

public abstract class TaggableItem
{
    public Guid Id { get; set; }

    public ICollection<TagBase> Tags { get; set; } = new List<TagBase>();
}

public class TaggableFile : TaggableItem
{
    public required string Path { get; set; }
}

public class TaggableFolder : TaggableItem
{
    public required string Path { get; set; }
}
