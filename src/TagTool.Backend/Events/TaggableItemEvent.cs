using MediatR;

namespace TagTool.Backend.Events;

public abstract class TaggableItemChanged : INotification
{
    public abstract string EventName { get; }

    public required Guid TaggableItemId { get; init; }
}
