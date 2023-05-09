using FoxMud.Common.Interfaces;

namespace FoxMud.Common.Models
{
    public class PlayerContext
    {
        public IPlayerConnection Connection { get; set; }
        public Character? Character { get; set; }

        public PlayerContext(IPlayerConnection connection)
        {
            Connection = connection;
        }
    }
}
