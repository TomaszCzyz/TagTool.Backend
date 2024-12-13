using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.TaggableFile;

public class TaggableFile : ITaggableItem
{
    public ICollection<TagBase> Tags { get; } = [];

    // attributes?
}
