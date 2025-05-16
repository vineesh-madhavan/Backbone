//Backbone.Core/Security/SecurityExtensions.cs
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Core.Secutiy
{
    public static class SecurityExtensions
    {
        public static bool VerifyPasswordHash(this byte[] storedHash, byte[] storedSalt, string password, ILogger logger = null)
        {
            using var _ = logger?.BeginScope(new { Operation = "VerifyPasswordHash" });

            try
            {
                logger?.LogDebug("Starting password hash verification");

                if (storedHash == null)
                {
                    logger?.LogError("Stored hash is null");
                    throw new ArgumentNullException(nameof(storedHash));
                }

                if (storedSalt == null)
                {
                    logger?.LogError("Stored salt is null");
                    throw new ArgumentNullException(nameof(storedSalt));
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    logger?.LogError("Password is null or whitespace");
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
                }

                using var hmac = new HMACSHA512(storedSalt);
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                var isValid = CryptographicOperations.FixedTimeEquals(computedHash, storedHash);

                logger?.LogDebug("Password hash verification completed. Result: {IsValid}", isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during password hash verification");
                throw; // Re-throw to allow caller to handle
            }
        }

        public static (byte[] hash, byte[] salt) CreatePasswordHash(string password, ILogger logger = null)
        {
            using var _ = logger?.BeginScope(new { Operation = "CreatePasswordHash" });

            try
            {
                logger?.LogDebug("Creating new password hash");

                if (string.IsNullOrWhiteSpace(password))
                {
                    logger?.LogError("Password is null or whitespace");
                    throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
                }

                using var hmac = new HMACSHA512();
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                logger?.LogDebug("Successfully created password hash and salt");

                return (hash, salt);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during password hash creation");
                throw; // Re-throw to allow caller to handle
            }
        }
    }
}