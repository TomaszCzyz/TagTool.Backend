using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Contracts.Invocables.Common;

namespace TagTool.BackendNew.Invocables;

public class MoveToCommonStoragePayload : PayloadWithChangedItems
{
    public required string CommonStoragePath { get; set; }
}

public class MoveToCommonStorage : IEventTriggeredInvocable<MoveToCommonStoragePayload>
{
    private readonly ILogger<MoveToCommonStorage> _logger;

    public required MoveToCommonStoragePayload Payload { get; set; }

    public MoveToCommonStorage(ILogger<MoveToCommonStorage> logger)
    {
        _logger = logger;
    }

    public Task Invoke()
    {
        _logger.LogInformation("Moving files to common storage");
        return Task.CompletedTask;
    }
}
