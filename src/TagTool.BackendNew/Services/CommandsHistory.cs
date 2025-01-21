using TagTool.BackendNew.Contracts.Internal;

namespace TagTool.BackendNew.Services;

public interface ICommandsHistory
{
    void Push(IReversible command);

    IReversible? GetUndoCommand();

    IReversible? GetRedoCommand();
}

public class CommandsHistory : ICommandsHistory
{
    private readonly Stack<IReversible> _undoCommands = new();
    private readonly Stack<IReversible> _redoCommands = new();

    public void Push(IReversible command) => _undoCommands.Push(command.GetReverse());

    public IReversible? GetUndoCommand()
    {
        if (!_undoCommands.TryPop(out var undoCommand))
        {
            return null;
        }

        _redoCommands.Push(undoCommand.GetReverse());

        return undoCommand;
    }

    public IReversible? GetRedoCommand()
    {
        if (!_redoCommands.TryPop(out var redoCommand))
        {
            return null;
        }

        _undoCommands.Push(redoCommand.GetReverse());

        return redoCommand;
    }
}
