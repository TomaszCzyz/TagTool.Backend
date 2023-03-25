using TagTool.Backend.Models;
using TagTool.Backend.Models.Taggable;

namespace TagTool.Backend.Taggers;

public interface ITagger<in T> where T : ITaggable
{
    TaggedItem? Tag(T item, string[] tagNames);

    TaggedItem? Untag(T item, string[] tagNames);
}
