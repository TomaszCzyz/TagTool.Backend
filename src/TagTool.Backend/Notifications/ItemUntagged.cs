using Hangfire.Annotations;
using MediatR;

namespace TagTool.Backend.Notifications;

public sealed class ItemUntaggedNotification : INotification
{
    public required Guid UntaggedItemId { get; init; }

    public required int RemovedTagId { get; init; }
}

[UsedImplicitly]
public sealed class ItemUntagged : INotificationHandler<ItemUntaggedNotification>
{
    private readonly ILogger<ItemUntagged> _logger;

    public ItemUntagged(ILogger<ItemUntagged> logger)
    {
        _logger = logger;
    }

    public Task Handle(ItemUntaggedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
