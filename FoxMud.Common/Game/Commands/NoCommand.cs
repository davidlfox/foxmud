using FoxMud.Common.Models;

namespace FoxMud.Common.Game.Commands;

[Command("noopcommand")]
public class NoOpCommand : ICommand
{
    public int RoundsToComplete => 0;

    public string Arguments { get; set; } = string.Empty;

    public async Task ExecuteBeforeAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        await gameEngine.DisplayPrompt(playerContext);
    }

    public async Task ExecuteAfterAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        await Task.FromResult(0);
    }
}
