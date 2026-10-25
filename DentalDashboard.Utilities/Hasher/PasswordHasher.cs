using System;
using System.Security.Cryptography;
using System.Text;

namespace DentalDashboard.Utilities.Hasher
{
    public static class PasswordHasher
    {
        private const string Algorithm = "PBKDF2-SHA256";
        private const int Iterations = 210_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string HashPassword(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                if (storedHash.StartsWith(Algorithm + "$", StringComparison.Ordinal))
                    return VerifyPbkdf2(password, storedHash);

                return VerifyLegacySha256(password, storedHash);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static bool NeedsRehash(string storedHash)
        {
            if (!storedHash.StartsWith(Algorithm + "$", StringComparison.Ordinal))
                return true;

            var parts = storedHash.Split('$');
            return parts.Length != 4 ||
                   !int.TryParse(parts[1], out var iterations) ||
                   iterations < Iterations;
        }

        private static bool VerifyPbkdf2(string password, string storedHash)
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || parts[0] != Algorithm ||
                !int.TryParse(parts[1], out var iterations) ||
                iterations is < 10_000 or > 1_000_000)
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            if (salt.Length < SaltSize || expectedHash.Length < KeySize)
                return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static bool VerifyLegacySha256(string password, string storedHash)
        {
            var fullBytes = Convert.FromBase64String(storedHash);
            if (fullBytes.Length != SaltSize + KeySize)
                return false;

            var salt = fullBytes.AsSpan(0, SaltSize);
            var expectedHash = fullBytes.AsSpan(SaltSize, KeySize);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[passwordBytes.Length + SaltSize];
            passwordBytes.CopyTo(combined, 0);
            salt.CopyTo(combined.AsSpan(passwordBytes.Length));
            var actualHash = SHA256.HashData(combined);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
