using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Invocables;

public class MoveToCommonStoragePayload : PayloadWithChangedItems
{
    public required string CommonStoragePath { get; set; }
}
