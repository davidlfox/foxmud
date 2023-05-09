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
    private const int TickLength = 1000;

    static async Task Main(string[] args)
    {
        var dataStore = new FileDataStore("Data");
        var gameEngine = new GameEngine(dataStore, new CommandProcessor());

        // Start the tick system
        var tickDuration = TimeSpan.FromMilliseconds(TickLength);
        _ = StartTickSystemAsync(gameEngine, tickDuration);

        var listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"Listening on port {Port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected");
            var playerConnection = await PlayerConnection.CreateAsync(client, Port);
            _ = HandleConnection(new PlayerContext(playerConnection), gameEngine);
        }
    }

    public static async Task StartTickSystemAsync(GameEngine gameEngine, TimeSpan tickDuration)
    {
        while (true)
        {
            await gameEngine.TickAsync();
            await Task.Delay(tickDuration);
        }
    }

    private static async Task HandleConnection(PlayerContext context, GameEngine gameEngine)
    {
        IPlayerConnection connection = context.Connection;
        // Flush any existing data
        await connection.WriteLineAsync("\r\n");
        await connection.FlushAsync();

        context.Character = await gameEngine.Login(context);

        if (context.Character is null)
        {
            throw new NotImplementedException();
        }

        gameEngine.LoggedInPlayers.Add(context);
        await connection.WriteLineAsync($"Welcome, {context.Character.Name}!\r\n");
        await connection.FlushAsync();
        await DisplayPrompt(context);

        string input;
        while ((input = await context.Connection.ReadLineAsync()).ToLower() != "quit")
        {
            await gameEngine.HandleCommandAsync(context, input.ToLower());
            await DisplayPrompt(context);
        }

        await connection.WriteLineAsync("Goodbye!");
        await connection.FlushAsync();
        gameEngine.LoggedInPlayers.Remove(context);
        
        connection.Dispose();
    }

    static async Task DisplayPrompt(PlayerContext context)
    {
        // Send the prompt
        await context.Connection.WriteLineAsync($"[{context.Character?.Name} HP: 100/100 MP: 50/50]> ");
        await context.Connection.FlushAsync();
    }
}