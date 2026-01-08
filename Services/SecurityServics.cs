using System.Security.Cryptography;
using System.Text;

namespace Journal.Services;

public static class SecurityService
{
    public static string HashPin(string pin)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pin));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string pin, string storedHash)
        => HashPin(pin) == storedHash;
}
