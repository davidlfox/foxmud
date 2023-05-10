namespace FoxMud.Common.Game.Commands;

public class CommandQueue
{
    private Queue<ICommand> _commands = new Queue<ICommand>();

    public void Enqueue(ICommand command)
    {
        _commands.Enqueue(command);
    }

    public ICommand Dequeue()
    {
        return _commands.Dequeue();
    }

    public void Clear()
    {
        _commands.Clear();
    }

    public bool IsEmpty => !_commands.Any();
}
