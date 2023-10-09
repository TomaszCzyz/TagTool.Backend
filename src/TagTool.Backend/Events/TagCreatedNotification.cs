using Hangfire.Annotations;
using MediatR;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Events;

public sealed class TagCreatedNotification : INotification
{
    public required TagBase Tag { get; init; }
}

[UsedImplicitly]
public sealed class TagCreated : INotificationHandler<TagCreatedNotification>
{
    private readonly ILogger<TagCreated> _logger;

    public TagCreated(ILogger<TagCreated> logger)
    {
        _logger = logger;
    }

    public Task Handle(TagCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {@Notification} notification...", notification);
        return Task.CompletedTask;
    }
}
