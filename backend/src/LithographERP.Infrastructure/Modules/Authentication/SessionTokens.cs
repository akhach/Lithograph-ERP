using System.Security.Cryptography;
using System.Text;

namespace LithographERP.Infrastructure.Modules.Authentication;

internal static class SessionTokens
{
    public static string Create() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
