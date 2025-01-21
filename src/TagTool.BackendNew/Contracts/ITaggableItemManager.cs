using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Contracts;

public interface ITaggableItemManager<in T> where T : TaggableItem
{
    Task<TaggableItem?> GetItem(T item, CancellationToken cancellationToken);
    Task<TaggableItem> GetOrAddItem(T item, CancellationToken cancellationToken);
}
