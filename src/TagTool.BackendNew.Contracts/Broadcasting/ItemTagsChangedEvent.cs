using Coravel.Events.Interfaces;

namespace TagTool.BackendNew.Contracts.Broadcasting;

public record ItemTagsChangedEvent(Guid ItemId, Dictionary<Guid, ChangeType> TagChanges) : IEvent;
