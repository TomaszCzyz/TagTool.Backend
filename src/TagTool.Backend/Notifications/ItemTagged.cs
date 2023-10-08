using Hangfire.Annotations;
using MediatR;

namespace TagTool.Backend.Notifications;

public sealed class ItemTaggedNotification : INotification
{
    public required Guid TaggedItemId { get; init; }

    public required int AddedTagId { get; init; }
}

[UsedImplicitly]
public sealed class ItemTagged : INotificationHandler<ItemTaggedNotification>
{
    private readonly ILogger<ItemTagged> _logger;

    public ItemTagged(ILogger<ItemTagged> logger)
    {
        _logger = logger;
    }

    public Task Handle(ItemTaggedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
