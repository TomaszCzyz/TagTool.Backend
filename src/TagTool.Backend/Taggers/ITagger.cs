using TagTool.Backend.Models.Taggable;

namespace TagTool.Backend.Taggers;

public interface ITagger<T> where T : ITaggable
{
    Tagged<T>? Tag(T item, string[] tagNames);

    Tagged<T>? Untag(T item, string[] tagNames);
}
