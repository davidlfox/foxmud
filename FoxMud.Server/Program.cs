using System.Net;
using System.Net.Sockets;
using ANSIConsole;
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
        var dataStore = new FileDataStore("Data");
        var gameEngine = new GameEngine(dataStore);

        // Start the tick system
        var tickDuration = TimeSpan.FromMilliseconds(gameEngine.TickLength);
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
        var cancellationToken = new CancellationTokenSource();
        var processTasks = gameEngine.ProcessPlayerCommandsAsync(context, cancellationToken.Token);
        await connection.WriteLineAsync("\x1b[2J"); // clear screen
        await connection.FlushAsync();
        await connection.WriteLineAsync($"Welcome, {context.Character.Name}!\r\n".Color(ConsoleColor.Green).ToString());
        await connection.FlushAsync();
        
        await gameEngine.DisplayPrompt(context);

        string input;
        while ((input = await context.Connection.ReadLineAsync()).ToLower() != "quit")
        {
            await gameEngine.HandleCommandAsync(context, input.ToLower());
        }

        await connection.WriteLineAsync("Goodbye!");
        await connection.FlushAsync();
        gameEngine.LoggedInPlayers.Remove(context);
        cancellationToken.Cancel();
        
        connection.Dispose();
    }
}