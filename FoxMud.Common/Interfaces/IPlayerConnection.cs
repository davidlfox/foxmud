namespace FoxMud.Common.Interfaces
{
    public interface IPlayerConnection
    {
        Task<string> ReadLineAsync();
        Task WriteAsync(string message);
        Task WriteLineAsync(string message);
        Task FlushAsync();
        void Dispose();
    }
}