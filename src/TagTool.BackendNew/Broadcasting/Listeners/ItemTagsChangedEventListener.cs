using System.Text.Json;
using Coravel.Events.Interfaces;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Broadcasting.Listeners;

public class ItemTagsChangedEventListener : IListener<ItemTagsChangedEvent>
{
    private readonly ILogger<ItemTagsChangedEventListener> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITagToolDbContext _dbContext;

    public ItemTagsChangedEventListener(
        ILogger<ItemTagsChangedEventListener> logger,
        IServiceProvider serviceProvider,
        ITagToolDbContext dbContext)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
    }

    public Task HandleAsync(ItemTagsChangedEvent broadcasted)
    {
        _logger.LogInformation("Tags of Item {ItemId} has changed... executing Jobs", broadcasted.ItemId);
        _logger.LogDebug("Tags changed: {@Notification}", broadcasted.TagChanges);

        foreach (var info in _dbContext.EventTriggeredInvocableInfos)
        {
            var queuingHandlerType = typeof(IQueuingHandler<,>).MakeGenericType(info.InvocableType, info.InvocablePayloadType);
            var queuingHandler = _serviceProvider.GetRequiredService(queuingHandlerType);

            if (queuingHandler is not IQueuingHandlerBase handler)
            {
                throw new ArgumentException("Incorrect QueuingHandler");
            }

            var payload = JsonSerializer.Deserialize(info.Payload, info.InvocablePayloadType);

            if (payload is null)
            {
                _logger.LogError("Unable to deserialize payload");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Queuing event-triggered invocable {@InvocableInfo}", info);
            handler.Queue(payload);
        }

        return Task.CompletedTask;
    }
}
