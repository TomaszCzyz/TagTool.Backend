namespace TagTool.BackendNew.Entities;

internal interface ITaggableItem
{
    // ? complex ID with column 'TaggableItemType' (e.g. file) and 'CustomIdentifier' (e.g. path)

    ICollection<TagBase> Tags { get; }
}

public abstract class TaggableItem : ITaggableItem
{
    public Guid Id { get; set; }

    public ICollection<TagBase> Tags { get; set; } = new List<TagBase>();
}
