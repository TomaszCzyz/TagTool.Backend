using Coravel.Invocable;
using TagTool.BackendNew.Invocables.Common;

namespace TagTool.BackendNew.Invocables;

public interface IEventTriggeredInvocable<TPayload> : IInvocable, IInvocableWithPayload<TPayload>
    where TPayload : PayloadWithChangedItems;

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
