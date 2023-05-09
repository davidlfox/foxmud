using FoxMud.Common.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FoxMud.Common.Server
{
    public class PlayerConnection : IPlayerConnection, IDisposable
    {
        private readonly TcpClient _client;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private PlayerConnection(TcpClient client)
        {
            _client = client;
            var stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            _writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        }

        public static async Task<PlayerConnection> CreateAsync(TcpClient client, int port)
        {
            var playerConnection = new PlayerConnection(client);
            return await Task.FromResult(playerConnection);
        }

        public async Task<string> ReadLineAsync()
        {
            var builder = new StringBuilder();
            var buffer = new Memory<char>(new char[1]);

            while (true)
            {
                await _reader.ReadAsync(buffer);
                char ch = buffer.Span[0];
                if (ch == '\n') break;
                if (ch == '\r') continue;
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public Task WriteAsync(string message)
        {
            return _writer.WriteAsync(message);
        }

        public Task WriteLineAsync(string message)
        {
            return _writer.WriteLineAsync(message);
        }

        public Task FlushAsync()
        {
            return _writer.FlushAsync();
        }

        public void Dispose()
        {
            _reader.Dispose();
            _writer.Dispose();
            _client.Close();
        }
    }
}
