using JetBrains.Annotations;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Invocables;

[UsedImplicitly]
public class CronMoveToCommonStorageInvocableDescription : IInvocableDescription<CronMoveToCommonStorage>
{
    public string Id { get; } = "FF0969B2";
    public string GroupId { get; } = "MoveToCommonStorage:EFC6";
    public string DisplayName { get; } = "Move to common storage";
    public string Description { get; } = "Moves file tagged with specific tag to given folder";
}
