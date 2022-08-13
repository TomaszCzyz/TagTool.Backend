namespace TagTool.Backend.Commands;

public interface ICommandInvoker
{
    void SetCommand(ICommand command);
    Task SetAndInvoke(ICommand command);
    Task Invoke();
    Task Undo();
    Task Redo();
}

public class CommandInvoker : ICommandInvoker
{
    private readonly CommandHistory _commandHistory = new();
    private readonly CommandHistory _undoCommandHistory = new();
    private ICommand? _command;

    public void SetCommand(ICommand command)
    {
        _command = command;
    }

    public async Task SetAndInvoke(ICommand command)
    {
        SetCommand(command);
        await Invoke();
    }

    public async Task Invoke()
    {
        if (_command is null) return;

        _commandHistory.Push(_command);
        _undoCommandHistory.Clear();

        await _command.Execute();
    }

    public async Task Undo()
    {
        if (!_commandHistory.TryPop(out var command)) return;

        // command.Undo()

        _undoCommandHistory.Push(command);
    }

    public Task Redo()
    {
        if (!_commandHistory.TryPop(out var command)) return Task.CompletedTask;

        command.Execute();

        _commandHistory.Push(command);
        return Task.CompletedTask;
    }
}
