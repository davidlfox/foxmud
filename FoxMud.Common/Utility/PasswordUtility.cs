using System.Security.Cryptography;
using System.Text;

namespace FoxMud.Common.Utility;

public static class PasswordUtility
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
}
