using System;
using System.Security.Cryptography;
using System.Text;

namespace RealTimeInventoryBilling.API.Services
{
    public static class PasswordSecurity
    {
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            byte[] combinedBytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = sha256.ComputeHash(combinedBytes);
            return Convert.ToHexString(hashBytes);
        }

        public static bool VerifyPassword(string enteredPassword, string salt, string storedHash)
        {
            string computedHash = HashPassword(enteredPassword, salt);
            return string.Equals(computedHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
