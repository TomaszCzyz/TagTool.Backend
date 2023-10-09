using Hangfire.Annotations;
using MediatR;

namespace TagTool.Backend.Events;

public sealed class ItemTaggedNotif : ITaggableItemNotif
{
    public required Guid TaggableItemId { get; init; }

    public required int AddedTagId { get; init; }
}

[UsedImplicitly]
public sealed class ItemTagged : INotificationHandler<ItemTaggedNotif>
{
    private readonly ILogger<ItemTagged> _logger;

    public ItemTagged(ILogger<ItemTagged> logger)
    {
        _logger = logger;
    }

    public Task Handle(ItemTaggedNotif notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
