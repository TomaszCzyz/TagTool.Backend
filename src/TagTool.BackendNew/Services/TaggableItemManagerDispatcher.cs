using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.TaggableFile;

namespace TagTool.BackendNew.Services;

public class TaggableItemManagerDispatcher : ITaggableItemManager<TaggableItem>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaggableItemManagerDispatcher(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TaggableItem?> GetItem(TaggableItem item, CancellationToken cancellationToken)
    {
        switch (item)
        {
            case TaggableFile.TaggableFile file:
                var taggableFileManager = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TaggableFileManager>();
                return taggableFileManager.GetItem(file, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, null);
        }
    }

    public Task<TaggableItem> GetOrAddItem(TaggableItem item, CancellationToken cancellationToken)
    {
        switch (item)
        {
            case TaggableFile.TaggableFile file:
                var taggableFileManager = _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<TaggableFileManager>();
                return taggableFileManager.GetOrAddItem(file, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(item), item, null);
        }
    }
}
