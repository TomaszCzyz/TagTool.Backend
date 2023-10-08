using Hangfire.Annotations;
using MediatR;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Notifications;

public sealed class TagDeletedNotification : INotification
{
    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public sealed class TagDeleted : INotificationHandler<TagDeletedNotification>
{
    private readonly ILogger<TagDeleted> _logger;

    public TagDeleted(ILogger<TagDeleted> logger)
    {
        _logger = logger;
    }

    public Task Handle(TagDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
