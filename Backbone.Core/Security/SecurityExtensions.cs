using System.Security.Cryptography;
using System.Text;

namespace Core.Secutiy
{
    public static class SecurityExtensions
    {
        public static bool VerifyPasswordHash(this byte[] storedHash, byte[] storedSalt, string password)
        {
            if (storedHash == null) throw new ArgumentNullException(nameof(storedHash));
            if (storedSalt == null) throw new ArgumentNullException(nameof(storedSalt));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));

            using var hmac = new HMACSHA512(storedSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }

        public static (byte[] hash, byte[] salt) CreatePasswordHash(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return (hash, salt);
        }
    }
}