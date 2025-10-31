using System.Security.Cryptography;
using System.Text;

namespace Hartonomous.Core.Utilities;

public static class HashUtility
{
    public static string ComputeSHA256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static byte[] ComputeSHA256Bytes(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }
}
