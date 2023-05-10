using FoxMud.Common.Models;

namespace FoxMud.Common.Game.Commands;

[Command("look")]
public class LookCommand : ICommand
{
    public int RoundsToComplete => 1; // This command takes 1 round to complete
    public string Arguments { get; set; } = string.Empty;

    public async Task ExecuteBeforeAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        // Describe the room to the player
        var roomDescription = "The void is all around you.";
        await playerContext.Connection.WriteLineAsync(roomDescription);
        await playerContext.Connection.FlushAsync();
        await gameEngine.DisplayPrompt(playerContext);
    }

    public async Task ExecuteAfterAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        await Task.FromResult(0);
    }
}
