using System.Diagnostics.CodeAnalysis;

namespace TagTool.Backend.Commands;

public class CommandHistory
{
    private readonly Stack<ICommand> _commands = new();

    public void Push(ICommand command)
    {
        _commands.Push(command);
    }

    public bool TryPop([NotNullWhen(true)]out ICommand? command)
    {
        return _commands.TryPop(out command);
    }

    public void Clear()
    {
        _commands.Clear();
    }
}
