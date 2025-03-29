namespace TagTool.BackendNew.Contracts.Invocables;

public interface ITaggableItemManagerBase
{
    Task<TaggableItem?> GetItem(TaggableItem item, CancellationToken cancellationToken);
    Task<TaggableItem> GetOrAddItem(TaggableItem item, CancellationToken cancellationToken);
}

public interface ITaggableItemManager<in T> : ITaggableItemManagerBase where T : TaggableItem
{
    Task<TaggableItem?> ITaggableItemManagerBase.GetItem(TaggableItem item, CancellationToken cancellationToken)
        => GetItem((T)item, cancellationToken);

    Task<TaggableItem> ITaggableItemManagerBase.GetOrAddItem(TaggableItem item, CancellationToken cancellationToken)
        => GetOrAddItem((T)item, cancellationToken);

    Task<TaggableItem?> GetItem(T item, CancellationToken cancellationToken);
    Task<TaggableItem> GetOrAddItem(T item, CancellationToken cancellationToken);
}
