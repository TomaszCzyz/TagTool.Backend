using TagTool.Backend.Models.Taggable;

namespace TagTool.Backend.Taggers;

public interface ITagger<T> where T : ITaggable
{
    Tagged<T>? Tag(T item, string[] tagNames, TagOptions? options = null);

    Tagged<T>? Untag(T item, string[] tagNames, TagOptions? options = null);
}

public class TagOptions
{
    public FolderTaggingDepth FolderTaggingDepth { get; init; }
}

public enum FolderTaggingDepth
{
    None,
    One,
    All
}
