using FoxMud.Common.Models;

namespace FoxMud.Common.Game.Commands;

[Command("say")]
public class SayCommand : ICommand
{
    public int RoundsToComplete => 0; // This command executes instantly
    public string Arguments { get; set; } = string.Empty;

    public async Task ExecuteBeforeAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        // Send the message to all players in the same room
        foreach (var player in gameEngine.LoggedInPlayers)
        {
            if (player.Character?.Name == playerContext.Character?.Name)
            {
                await playerContext.Connection.WriteLineAsync($"You say, \"{this.Arguments}\"");
                await playerContext.Connection.FlushAsync();
                await gameEngine.DisplayPrompt(playerContext);
            }
            else
            {
                await player.Connection.WriteLineAsync($"{playerContext.Character?.Name} says, \"{this.Arguments}\"");
                await player.Connection.FlushAsync();
                await gameEngine.DisplayPrompt(player);
            }
        }
    }

    public async Task ExecuteAfterAsync(PlayerContext playerContext, GameEngine gameEngine)
    {
        await Task.FromResult(0);
    }
}
