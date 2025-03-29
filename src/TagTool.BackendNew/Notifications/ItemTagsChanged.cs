using MediatR;
using TagTool.BackendNew.Contracts.Broadcasting;

namespace TagTool.BackendNew.Notifications;

public record ItemTagsChanged(Guid ItemId, Dictionary<Guid, ChangeType> TagChanges) : INotification;
