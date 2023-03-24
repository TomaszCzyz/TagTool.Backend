namespace TagTool.Backend.Models.Taggable;

public class TaggedItem<T> where T : ITaggable
{
    public required T Item { get; init; }

    public required ISet<Tag> Tags { get; init; }
}
