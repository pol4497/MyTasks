using System.Security.Cryptography;

namespace MyTasks.Security
{
    /// <summary>
    /// PBKDF2-based password hashing. Uses only BCL cryptography APIs.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSizeBytes = 16;   // 128-bit salt
        private const int KeySizeBytes = 32;    // 256-bit derived key
        private const int Iterations = 100_000; // OWASP-recommended minimum for PBKDF2-SHA256
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hashes a plaintext password. The result encodes the iteration count, salt,
        /// and derived key together, so it can be verified without any extra config.
        /// Format: {iterations}.{salt-base64}.{key-base64}
        /// </summary>
        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySizeBytes);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        /// <summary>
        /// Verifies a plaintext password against a hash produced by <see cref="Hash"/>.
        /// </summary>
        public static bool Verify(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.', 3);
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out var iterations)) return false;

            byte[] salt, expectedKey;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedKey = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }
}
