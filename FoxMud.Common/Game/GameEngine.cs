using System.Reflection;
using FoxMud.Common.Game.Commands;
using FoxMud.Common.Interfaces;
using FoxMud.Common.Models;
using FoxMud.Common.Utility;

namespace FoxMud.Common.Game
{
    public class GameEngine
    {
        private readonly IDataStore _db;
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();
        private int tickNumber = 0;
        public int TickLength = 1000;
        private int CommandCheckInterval = 50;

        public List<PlayerContext> LoggedInPlayers { get; } = new List<PlayerContext>();


        public GameEngine(IDataStore db)
        {
            _db = db;
            LoadCommands();
        }

        public async Task TickAsync()
        {
            // perform game state updates and send global tick announcements to players
            await Task.FromResult(tickNumber++);
            Console.WriteLine($"tick: {tickNumber}");
        }

        public async Task HandleCommandAsync(PlayerContext context, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                if (context.CommandQueue.IsEmpty)
                {
                    await DisplayPrompt(context);
                }
                else
                {
                    context.CommandQueue.Enqueue(new NoOpCommand());
                }
            }
            else
            {
                var command = ParseCommand(input);
                if (command is null)
                {
                    await context.Connection.WriteLineAsync("Huh?");
                    await context.Connection.FlushAsync();
                }
                else
                {
                    context.CommandQueue.Enqueue(command);
                }
            }
        }

        public async Task ProcessPlayerCommandsAsync(PlayerContext playerContext, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!playerContext.CommandQueue.IsEmpty)
                {
                    var nextCommand = playerContext.CommandQueue.Dequeue();
                    var roundsRemaining = nextCommand.RoundsToComplete;

                    await nextCommand.ExecuteBeforeAsync(playerContext, this);

                    while (roundsRemaining > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(this.TickLength));
                        roundsRemaining--;
                    }

                    await nextCommand.ExecuteAfterAsync(playerContext, this);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(this.CommandCheckInterval));
                }
            }
        }

        public async Task DisplayPrompt(PlayerContext context)
        {
            // Send the prompt
            await context.Connection.WriteLineAsync($"[{context.Character?.Name} HP: 100/100 MP: 50/50]> ");
            await context.Connection.FlushAsync();
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

        private void LoadCommands()
        {
            var commandTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && t.GetCustomAttribute<CommandAttribute>() != null);

            foreach (var type in commandTypes)
            {
                var attribute = type.GetCustomAttribute<CommandAttribute>();
                var instance = Activator.CreateInstance(type) as ICommand;
                if (attribute is null || instance is null) continue;
                if (attribute.CommandName == "huh") continue;
                _commands.Add(attribute.CommandName, instance);
            }
        }

        public ICommand? ParseCommand(string input)
        {
            var split = input.Split(' ');
            var commandName = split[0].ToLower();
            if (_commands.TryGetValue(commandName, out var command))
            {
                if (split.Length > 1)
                {
                    command.Arguments = input.Replace(input.Split(' ')[0], "").Trim();
                }

                return command;
            }

            return null;
        }
    }
}
