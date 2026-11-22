using System.Security.Cryptography;
using System.Text;
using Project1.Core.Interfaces;

namespace Project1.Data.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(salt, password);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var expected = Convert.FromBase64String(hash);
            var actual = ComputeHash(saltBytes, password);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }

    private static byte[] ComputeHash(byte[] salt, string password)
    {
        using var sha = SHA256.Create();
        var pwdBytes = Encoding.UTF8.GetBytes(password);
        var buffer = new byte[salt.Length + pwdBytes.Length];
        Buffer.BlockCopy(salt, 0, buffer, 0, salt.Length);
        Buffer.BlockCopy(pwdBytes, 0, buffer, salt.Length, pwdBytes.Length);
        return sha.ComputeHash(buffer);
    }
}