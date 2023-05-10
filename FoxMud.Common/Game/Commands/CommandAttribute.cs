namespace FoxMud.Common.Game.Commands;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string CommandName { get; }

    public CommandAttribute(string commandName)
    {
        CommandName = commandName;
    }
}
