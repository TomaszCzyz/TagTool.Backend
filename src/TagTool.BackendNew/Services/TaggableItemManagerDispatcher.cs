using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.Services;

public class TaggableItemManagerDispatcher : ITaggableItemManager<TaggableItem>
{
    private readonly ILogger<TaggableItemManagerDispatcher> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly Dictionary<Type, Type> _taggableItemManagers;

    public TaggableItemManagerDispatcher(ILogger<TaggableItemManagerDispatcher> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _taggableItemManagers = typeof(Program).Assembly.ExportedTypes
            .Where(x => typeof(ITaggableItemManagerBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Select(type
                => type
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITaggableItemManager<>))
                    .GetGenericArguments()
                    .First())
            .Select(type => (ItemType: type, ManagerType: typeof(ITaggableItemManager<>).MakeGenericType(type)))
            .ToDictionary(tuple => tuple.ItemType, tuple => tuple.ManagerType);

        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task<TaggableItem?> GetItem(TaggableItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        if (_taggableItemManagers.TryGetValue(item.GetType(), out var managerType))
        {
            var taggableItemManager = (ITaggableItemManagerBase)scope.ServiceProvider.GetRequiredService(managerType);
            return taggableItemManager.GetItem(item, cancellationToken);
        }

        throw new InvalidOperationException($"No manager found for type {item.GetType()}");
    }

    public Task<TaggableItem> GetOrAddItem(TaggableItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        if (_taggableItemManagers.TryGetValue(item.GetType(), out var managerType))
        {
            var taggableItemManager = (ITaggableItemManagerBase)scope.ServiceProvider.GetRequiredService(managerType);
            return taggableItemManager.GetOrAddItem(item, cancellationToken);
        }

        throw new InvalidOperationException($"No manager found for type {item.GetType()}");
    }
}
