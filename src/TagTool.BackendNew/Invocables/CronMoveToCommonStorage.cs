using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Invocables.Common;
using TagTool.BackendNew.Services.Grpc.Dtos;

namespace TagTool.BackendNew.Invocables;

public class CronMoveToCommonStoragePayload : PayloadWithQuery
{
    public required string CommonStoragePathString { get; set; }

    public required DirectoryInfo CommonStoragePath { get; set; }

    public required Tag CommonStorageTag { get; set; }
}

public class CronMoveToCommonStorage : ICronTriggeredInvocable<CronMoveToCommonStoragePayload>
{
    private readonly ILogger<MoveToCommonStorage> _logger;

    public required CronMoveToCommonStoragePayload Payload { get; set; }

    public CronMoveToCommonStorage(ILogger<MoveToCommonStorage> logger)
    {
        _logger = logger;
    }

    public Task Invoke()
    {
        using var beginScope = _logger.BeginScope(new Dictionary<string, object> { ["JobName"] = nameof(CronMoveToCommonStorage) });
        _logger.LogInformation("Moving files to common storage");
        return Task.CompletedTask;
    }
}
