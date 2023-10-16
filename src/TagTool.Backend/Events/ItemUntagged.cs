namespace TagTool.Backend.Events;

public sealed class ItemUntaggedChanged : TaggableItemChanged
{
    public override string EventName { get; } = "ItemUntagged";

    public required int RemovedTagId { get; init; }
}

// [UsedImplicitly]
// public sealed class ItemUntagged : INotificationHandler<ItemUntaggedChanged>
// {
//     private readonly ILogger<ItemUntagged> _logger;
//
//     public ItemUntagged(ILogger<ItemUntagged> logger)
//     {
//         _logger = logger;
//     }
//
//     public Task Handle(ItemUntaggedChanged notification, CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Handling {@Notification} notification...", notification);
//         return Task.CompletedTask;
//     }
// }
