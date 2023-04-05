using MediatR;
using OneOf;
using TagTool.Backend.Commands;

namespace TagTool.Backend.Services;

public interface ICommandsHistory
{
    void Push<TResponse>(ICommand<TResponse> command) where TResponse : IOneOf;

    // ICommand<TResponse> Pop<TResponse>() where TResponse : IOneOf;
    IBaseRequest Pop();
}

public class CommandsHistory : ICommandsHistory
{
    private readonly Stack<IBaseRequest> _commandsHistory = new();
    private readonly Stack<IBaseRequest> _undoCommandsHistory = new();

    public void Push<TResponse>(ICommand<TResponse> command) where TResponse : IOneOf
    {
        _commandsHistory.Push(command);
        _undoCommandsHistory.Push(command.GetUndoCommand());
    }

    public IBaseRequest Pop()// where TResponse : IOneOf
    {
        var command = _commandsHistory.Pop();
        var undoCommand = _undoCommandsHistory.Pop();

        return undoCommand;
    }
}
