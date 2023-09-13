using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Models;

public abstract class TaggableItem
{
    public Guid Id { get; set; }

    /// <summary>
    ///     A score that keeps information about things like how many times an item has been used.
    /// </summary>
    public uint Popularity { get; set; }

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
