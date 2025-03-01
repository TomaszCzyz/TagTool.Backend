using Coravel.Events.Interfaces;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Broadcasting.Listeners;

public class ItemTagsChangedEventListener : IListener<ItemTagsChangedEvent>
{
    private readonly ILogger<ItemTagsChangedEventListener> _logger;
    private readonly TagToolDbContext _dbContext;
    private readonly IEventTriggeredInvocablesStorage _eventTriggeredInvocablesStorage;
    private readonly IServiceProvider _serviceProvider;

    public ItemTagsChangedEventListener(
        ILogger<ItemTagsChangedEventListener> logger,
        TagToolDbContext dbContext,
        IEventTriggeredInvocablesStorage eventTriggeredInvocablesStorage, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _dbContext = dbContext;
        _eventTriggeredInvocablesStorage = eventTriggeredInvocablesStorage;
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(ItemTagsChangedEvent broadcasted)
    {
        _logger.LogInformation("Tags of Item {ItemId} has changed... executing Jobs", broadcasted.ItemId);
        _logger.LogDebug("Tags changed: {@Notification}", broadcasted.TagChanges);

        var payloads = await _eventTriggeredInvocablesStorage.GetPayloads(ItemTaggedTrigger.Instance);

        foreach (var (invocableType, payloadType, payload) in payloads)
        {
            // payload.
            var queuingHandlerType = Type.MakeGenericSignatureType(typeof(QueuingHandler<,>), invocableType, payloadType);
            var queuingHandler = _serviceProvider.GetRequiredService(queuingHandlerType);

            if (queuingHandler is not IQueuingHandlerBase handler)
            {
                throw new ArgumentException("Incorrect QueuingHandler");
            }

            handler.Queue(payload);
        }
    }
}
