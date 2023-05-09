using System.Net;
using System.Net.Sockets;
using FoxMud.Common.Data;
using FoxMud.Common.Game;
using FoxMud.Common.Interfaces;
using FoxMud.Common.Models;
using FoxMud.Common.Server;

namespace FoxMud.Server;

class Program
{
    private const int Port = 8888;

    static async Task Main(string[] args)
    {
        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"Listening on port {Port}");

        var dataStore = new FileDataStore("Data");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected");
            var playerConnection = await PlayerConnection.CreateAsync(client, Port);
            _ = HandleConnection(new PlayerContext(playerConnection), dataStore);
        }
    }

    private static async Task HandleConnection(PlayerContext context, IDataStore dataStore)
    {
        IPlayerConnection connection = context.Connection;
        // Flush any existing data
        await connection.WriteLineAsync("\r\n");
        await connection.FlushAsync();

        var gameEngine = new GameEngine();

        context.Character = await gameEngine.Login(context, dataStore);

        if (context.Character is null)
        {
            throw new NotImplementedException();
        }

        await connection.WriteLineAsync($"Welcome, {context.Character.Name}!\r\n");
        await connection.FlushAsync();

        while (true)
        {
            // Send the prompt
            await connection.WriteLineAsync($"[{context.Character.Name} HP: 100/100 MP: 50/50]> ");
            await connection.FlushAsync();

            string command = await connection.ReadLineAsync();

            if (command.Trim().ToLower() == "quit")
            {
                await connection.WriteLineAsync("Goodbye!");
                await connection.FlushAsync();
                break;
            }
            // Add command parsing and processing here.
        }

        connection.Dispose();
    }
}