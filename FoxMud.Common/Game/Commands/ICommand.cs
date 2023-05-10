using FoxMud.Common.Models;

namespace FoxMud.Common.Game.Commands;

public interface ICommand
{
    int RoundsToComplete { get; }
    string Arguments { get; set; }
    Task ExecuteBeforeAsync(PlayerContext playerContext, GameEngine gameEngine);
    Task ExecuteAfterAsync(PlayerContext playerContext, GameEngine gameEngine);
}
