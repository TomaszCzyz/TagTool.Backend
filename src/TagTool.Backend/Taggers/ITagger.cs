using TagTool.Backend.Models.Taggable;

namespace TagTool.Backend.Taggers;

public interface ITagger<T> where T : ITaggable
{
    TaggedItem<T>? Tag(T item, string[] tagNames);

    TaggedItem<T>? Untag(T item, string[] tagNames);
}
