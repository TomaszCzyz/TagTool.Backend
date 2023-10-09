using Hangfire.Annotations;
using MediatR;

namespace TagTool.Backend.Events;

public sealed class ItemUntaggedNotif : ITaggableItemNotif
{
    public required Guid TaggableItemId { get; init; }

    public required int RemovedTagId { get; init; }
}

[UsedImplicitly]
public sealed class ItemUntagged : INotificationHandler<ItemUntaggedNotif>
{
    private readonly ILogger<ItemUntagged> _logger;

    public ItemUntagged(ILogger<ItemUntagged> logger)
    {
        _logger = logger;
    }

    public Task Handle(ItemUntaggedNotif notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
