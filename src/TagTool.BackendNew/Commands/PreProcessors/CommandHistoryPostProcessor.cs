using JetBrains.Annotations;
using MediatR.Pipeline;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.Services;

namespace TagTool.BackendNew.Commands.PreProcessors;

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
        if (request is IReversible reversible && response.Value is not Error<string>)
        {
            _logger.LogInformation("PreProcessing request {@Request}", reversible);
            _commandsHistory.Push(reversible);
        }

        return Task.CompletedTask;
    }
}
