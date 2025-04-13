using Microsoft.Extensions.Logging;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Contracts.Invocables.Common;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Invocables;

public class CronMoveToCommonStoragePayload : PayloadWithQuery
{
    public required string CommonStoragePathString { get; set; }

    [SpecialType(Type = SpecialTypeAttribute.Kind.DirectoryPath)]
    public required string Path { get; set; }

    [SpecialType(Type = SpecialTypeAttribute.Kind.SingleTag)]
    public required int TagId { get; set; }
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
