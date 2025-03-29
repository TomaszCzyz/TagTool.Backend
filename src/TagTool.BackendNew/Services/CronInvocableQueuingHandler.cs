using System.Text.Json;
using Coravel.Invocable;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Services;

public class CronInvocableQueuingHandler : IInvocable
{
    private readonly ILogger<CronInvocableQueuingHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITagToolDbContext _dbContext;

    private readonly Guid _invocableId;

    public CronInvocableQueuingHandler(
        ILogger<CronInvocableQueuingHandler> logger,
        ITagToolDbContext dbContext,
        IServiceProvider serviceProvider,
        Guid invocableId)
    {
        _logger = logger;
        _dbContext = dbContext;
        _invocableId = invocableId;
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke()
    {
        var info = await _dbContext.CronTriggeredInvocableInfos.FindAsync(_invocableId);
        if (info is null)
        {
            _logger.LogError("Invocable not found");
            return;
        }

        var queuingHandlerType = typeof(IQueuingHandler<,>).MakeGenericType(info.InvocableType, info.InvocablePayloadType);
        var queuingHandler = _serviceProvider.GetRequiredService(queuingHandlerType);

        if (queuingHandler is not IQueuingHandlerBase handler)
        {
            throw new ArgumentException("Incorrect QueuingHandler");
        }

        // use invocableInfo.TagQuery to fetch items here
        // invocableInfo.Payload

        object? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize(info.Payload, info.InvocablePayloadType);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to deserialize payload");
        }

        if (payload is null)
        {
            _logger.LogError("Unable to deserialize payload");
            return;
        }

        _logger.LogInformation("Queuing cron-triggered invocable {@InvocableInfo}", info);
        handler.Queue(payload);
    }
}
