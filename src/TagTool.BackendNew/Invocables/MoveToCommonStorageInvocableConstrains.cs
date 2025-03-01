using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Invocables;

public class MoveToCommonStorageInvocableConstrains : IInvocableConstrains<MoveToCommonStorage>
{
    public ITrigger[] AllowedTriggers { get; } = [ItemTaggedTrigger.Instance];

    public Type AllowedArgsType { get; } = typeof(MoveToCommonStoragePayload);
}
