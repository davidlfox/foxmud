using FoxMud.Common.Game.Commands;
using FoxMud.Common.Interfaces;

namespace FoxMud.Common.Models
{
    public class PlayerContext
    {
        public IPlayerConnection Connection { get; set; }
        public Character? Character { get; set; }
        public CommandQueue CommandQueue { get; } = new CommandQueue();

        public PlayerContext(IPlayerConnection connection)
        {
            Connection = connection;
        }
    }
}
