using FoxMud.Common.Interfaces;
using FoxMud.Common.Models;
using FoxMud.Common.Utility;

namespace FoxMud.Common.Game
{
    public class GameEngine
    {
        private readonly IDataStore _db;
        private CommandProcessor _commandProcessor;
        private int tickNumber = 0;

        public List<PlayerContext> LoggedInPlayers { get; } = new List<PlayerContext>();


        public GameEngine(IDataStore db, CommandProcessor commandProcessor)
        {
            _db = db;
            _commandProcessor = commandProcessor;
        }

        public async Task TickAsync()
        {
            // perform game state updates and send global tick announcements to players
            await Task.FromResult(tickNumber++);
            Console.WriteLine($"tick: {tickNumber}");
        }

        public async Task HandleCommandAsync(PlayerContext context, string input)
        {
            await _commandProcessor.ExecuteCommandAsync(context, input);
        }

        public async Task<Character?> Login(PlayerContext context)
        {
            bool loggedIn = false;
            Character? character = null;

            while (!loggedIn)
            {
                await context.Connection.WriteLineAsync("Enter a character name, or type 'new' to create a new character:");
                await context.Connection.FlushAsync();
                string input = await context.Connection.ReadLineAsync();

                if (input.ToLower() == "new")
                {
                    character = await NewCharacterProcess(context.Connection);
                    loggedIn = true;
                }
                else if (_db.CharacterExists(input))
                {
                    await context.Connection.WriteLineAsync("Enter your password:");
                    await context.Connection.FlushAsync();
                    
                    if (await isValidPassword(context, input))
                    {
                        character = await _db.LoadCharacterAsync(input);
                        loggedIn = true;
                    }
                    else
                    {
                        do
                        {
                            await context.Connection.WriteLineAsync("Incorrect password. Try again.");
                            await context.Connection.FlushAsync();

                            if (await isValidPassword(context, input))
                            {
                                character = await _db.LoadCharacterAsync(input);
                                loggedIn = true;
                            }
                        }
                        while (!loggedIn);
                    }
                }
                else
                {
                    await context.Connection.WriteLineAsync($"Character '{input}' does not exist.");
                }
            }

            await CheckAndHandleDuplicateLoginAsync(character);

            return character;
        }

        private async Task<bool> isValidPassword(PlayerContext context, string name)
        {
            string password = await context.Connection.ReadLineAsync();
            string passwordHash = PasswordUtility.HashPassword(password);

            var tempChar = await _db.LoadCharacterAsync(name);
            string? storedPasswordHash = tempChar?.PasswordHash;

            return storedPasswordHash == passwordHash;
        }

        private async Task CheckAndHandleDuplicateLoginAsync(Character? character)
        {
            var existingPlayer = LoggedInPlayers.FirstOrDefault(p => p.Character?.Name == character?.Name);
            if (existingPlayer != null)
            {
                await existingPlayer.Connection.WriteLineAsync("Another connection with the same name has been detected. Disconnecting...");
                await existingPlayer.Connection.FlushAsync();
                existingPlayer.Connection.Dispose();
                LoggedInPlayers.Remove(existingPlayer);
            }
        }

        public async Task<Character> NewCharacterProcess(IPlayerConnection connection)
        {

            await connection.WriteLineAsync("Enter your character's name:");
            await connection.FlushAsync();
            string name = await connection.ReadLineAsync();

            bool characterExists = _db.CharacterExists(name);

            while(characterExists || name.ToLower() == "new")
            {
                await connection.WriteLineAsync($"Character '{name}' already exists. Please choose another name.");
                await connection.FlushAsync();

                name = await connection.ReadLineAsync();
                characterExists = _db.CharacterExists(name);
            }

            await connection.WriteLineAsync("Enter your password:");
            await connection.FlushAsync();
            string password = await connection.ReadLineAsync();
            string passwordHash = PasswordUtility.HashPassword(password);

            await connection.WriteLineAsync("Choose your class:");
            await connection.FlushAsync();
            string @class = await connection.ReadLineAsync();

            await connection.WriteLineAsync("Choose your race:");
            await connection.FlushAsync();
            string race = await connection.ReadLineAsync();

            await connection.WriteLineAsync("Enter a visual description of your character:");
            await connection.FlushAsync();
            string desc = await connection.ReadLineAsync();

            // Save the new character
            if (await _db.CreateCharacterAsync(name, passwordHash, @class, race, desc))
            {
                await connection.WriteLineAsync($"Character '{name}' created!");
                await connection.FlushAsync();

                return new Character(name, passwordHash, @class, race, desc);
            }

            throw new NotImplementedException();
        }
    }
}
