using MediatR;

namespace TagTool.BackendNew.Notifications;

public enum ChangeType
{
    Added,
    Removed
}

public record ItemTagsChanged(Guid ItemId, Dictionary<Guid, ChangeType> TagChanges) : INotification;
