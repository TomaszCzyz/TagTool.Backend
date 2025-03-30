using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Helpers;

namespace TagTool.BackendNew.Services;

public class TaggableItemManagerDispatcher : ITaggableItemManager<TaggableItem>
{
    private readonly ILogger<TaggableItemManagerDispatcher> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TaggableItemManagerDispatcher(
        ILogger<TaggableItemManagerDispatcher> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TaggableItem?> GetItem(TaggableItem item, CancellationToken cancellationToken)
    {
        using var scope = GetManager(item, out var manager);
        return manager.GetItem(item, cancellationToken);
    }

    public Task<TaggableItem> GetOrAddItem(TaggableItem item, CancellationToken cancellationToken)
    {
        using var scope = GetManager(item, out var manager);
        return manager.GetOrAddItem(item, cancellationToken);
    }

    private IServiceScope GetManager(TaggableItem item, out ITaggableItemManagerBase manager)
    {
        var scope = _serviceScopeFactory.CreateScope();

        var itemType = item.GetType();
        var taggableItemType = TaggableItemsHelper.TaggableItemTypes[itemType];

        manager = scope.ServiceProvider.GetKeyedService<ITaggableItemManagerBase>(taggableItemType)
                  ?? throw new NotImplementedException($"Missing manager of type {itemType.FullName}");

        return scope;
    }
}
