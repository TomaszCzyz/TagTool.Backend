using MediatR;

namespace TagTool.Backend.Events;

public interface ITaggableItemNotif : INotification
{
    Guid TaggableItemId { get; init; }
}
