using Coravel.Events.Interfaces;
using TagTool.BackendNew.Notifications;

namespace TagTool.BackendNew.Broadcasting;

public record ItemTagsChangedEvent(Guid ItemId, Dictionary<Guid, ChangeType> TagChanges) : IEvent;
