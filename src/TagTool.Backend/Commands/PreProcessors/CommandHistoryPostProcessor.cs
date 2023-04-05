using JetBrains.Annotations;
using MediatR.Pipeline;
using OneOf;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands.PreProcessors;

[UsedImplicitly]
public class CommandHistoryPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : class, ICommand<TResponse> where TResponse : IOneOf
{
    private readonly ILogger<CommandHistoryPostProcessor<TRequest, TResponse>> _logger;
    private readonly ICommandsHistory _commandsHistory;

    public CommandHistoryPostProcessor(
        ILogger<CommandHistoryPostProcessor<TRequest, TResponse>> logger,
        ICommandsHistory commandsHistory)
    {
        _commandsHistory = commandsHistory;
        _logger = logger;
    }

    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        if (response.Value is not ErrorResponse)
        {
            _logger.LogInformation("PreProcessing request {@Request}", request);
            _commandsHistory.Push(request);
        }

        return Task.CompletedTask;
    }
}
