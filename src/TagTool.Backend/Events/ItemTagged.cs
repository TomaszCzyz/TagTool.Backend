namespace TagTool.Backend.Events;

public sealed class ItemTaggedChanged : TaggableItemChanged
{
    public override string EventName { get; } = "ItemTagged";

    public required int AddedTagId { get; init; }
}

// [UsedImplicitly]
// public sealed class ItemTagged : INotificationHandler<ItemTaggedChanged>
// {
//     private readonly ILogger<ItemTagged> _logger;
//
//     public ItemTagged(ILogger<ItemTagged> logger)
//     {
//         _logger = logger;
//     }
//
//     public Task Handle(ItemTaggedChanged notification, CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Handling {@Notification} notification...", notification);
//         return Task.CompletedTask;
//     }
// }
