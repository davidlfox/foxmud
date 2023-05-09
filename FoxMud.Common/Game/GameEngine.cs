using FoxMud.Common.Interfaces;
using FoxMud.Common.Models;
using FoxMud.Common.Utility;

namespace FoxMud.Common.Game
{
    public class GameEngine
    {
        public async Task<Character?> Login(PlayerContext context, IDataStore dataStore)
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
                    character = await NewCharacterProcess(context.Connection, dataStore);
                    loggedIn = true;
                }
                else if (dataStore.CharacterExists(input))
                {
                    await context.Connection.WriteLineAsync("Enter your password:");
                    await context.Connection.FlushAsync();
                    
                    if (await isValidPassword(context, dataStore, input))
                    {
                        character = await dataStore.LoadCharacterAsync(input);
                        loggedIn = true;
                    }
                    else
                    {
                        do
                        {
                            await context.Connection.WriteLineAsync("Incorrect password. Try again.");
                            await context.Connection.FlushAsync();

                            if (await isValidPassword(context, dataStore, input))
                            {
                                character = await dataStore.LoadCharacterAsync(input);
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

            return character;
        }

        private async Task<bool> isValidPassword(PlayerContext context, IDataStore dataStore, string name)
        {
            string password = await context.Connection.ReadLineAsync();
            string passwordHash = PasswordUtility.HashPassword(password);

            var tempChar = await dataStore.LoadCharacterAsync(name);
            string? storedPasswordHash = tempChar?.PasswordHash;

            return storedPasswordHash == passwordHash;
        }


        public async Task<Character> NewCharacterProcess(IPlayerConnection connection, IDataStore dataStore)
        {

            await connection.WriteLineAsync("Enter your character's name:");
            await connection.FlushAsync();
            string name = await connection.ReadLineAsync();

            bool characterExists = dataStore.CharacterExists(name);

            while(characterExists || name.ToLower() == "new")
            {
                await connection.WriteLineAsync($"Character '{name}' already exists. Please choose another name.");
                await connection.FlushAsync();

                name = await connection.ReadLineAsync();
                characterExists = dataStore.CharacterExists(name);
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
            if (await dataStore.CreateCharacterAsync(name, passwordHash, @class, race, desc))
            {
                await connection.WriteLineAsync($"Character '{name}' created!");
                await connection.FlushAsync();

                return new Character(name, passwordHash, @class, race, desc);
            }

            throw new NotImplementedException();
        }
    }
}
