using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace MyTasks.Security
{
    /// <summary>
    /// Argon2id-based password hashing via NSec.Cryptography (a binding around libsodium). Per OWASP's current
    /// Password Storage Cheat Sheet recommendation: memory-hard, resistant to GPU/ASIC cracking.
    ///
    /// Verify() also accepts hashes produced by the old PBKDF2 implementation, so existing
    /// accounts (e.g. the seeded Admin) aren't locked out - Hash() always writes the new
    /// Argon2id format going forward, but nothing rehashes old passwords automatically.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSizeBytes = 16; // matches libsodium's Argon2id salt size
        private const int HashSizeBytes = 32; // 256-bit derived key

        // OWASP's minimum-recommended Argon2id parameters (Password Storage Cheat Sheet):
        // 19 MiB memory, 2 passes. Parallelism is fixed at 1 - NSec's Argon2id binding
        // only supports 1 and throws for any other value.
        private const int Iterations = 2;
        private const int MemorySizeKB = 19_456;
        private const int Parallelism = 1;

        // Only used to verify hashes created before the Argon2id migration - never for new hashes.
        private static readonly HashAlgorithmName LegacyAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hashes a plaintext password with Argon2id. The result encodes the parameters,
        /// salt, and derived key together, so it can be verified without any extra config,
        /// and so future parameter changes don't break hashes created under the old ones.
        /// Format: {iterations}.{memoryKB}.{parallelism}.{salt-base64}.{hash-base64}
        /// </summary>
        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hash = DeriveArgon2id(password, salt, Iterations, MemorySizeKB, HashSizeBytes);

            return $"{Iterations}.{MemorySizeKB}.{Parallelism}." +
                   $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a plaintext password against a hash produced by <see cref="Hash"/>, or
        /// against a legacy PBKDF2 hash from before the Argon2id migration.
        /// </summary>
        public static bool Verify(string password, string hashedPassword)
        {
            var parts = hashedPassword.Split('.');

            return parts.Length switch
            {
                5 => VerifyArgon2id(password, parts),
                3 => VerifyLegacyPbkdf2(password, parts),
                _ => false
            };
        }

        private static bool VerifyArgon2id(string password, string[] parts)
        {
            if (!int.TryParse(parts[0], out var iterations)) return false;
            if (!int.TryParse(parts[1], out var memorySizeKB)) return false;
            // parts[2] (parallelism) is always 1 for this implementation - parsed only to
            // keep the format symmetric with Hash()'s output.
            if (!int.TryParse(parts[2], out _)) return false;

            byte[] salt, expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[3]);
                expectedHash = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualHash = DeriveArgon2id(password, salt, iterations, memorySizeKB, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static byte[] DeriveArgon2id(
            string password, byte[] salt, int iterations, int memorySizeKB, int hashSizeBytes)
        {
            var parameters = new Argon2Parameters
            {
                DegreeOfParallelism = Parallelism,
                MemorySize = memorySizeKB,
                NumberOfPasses = iterations
            };

            var algorithm = PasswordBasedKeyDerivationAlgorithm.Argon2id(parameters);

            return algorithm.DeriveBytes(Encoding.UTF8.GetBytes(password), salt, hashSizeBytes);
        }

        // --- Legacy PBKDF2 support (verification only - never used for new hashes) ---

        private static bool VerifyLegacyPbkdf2(string password, string[] parts)
        {
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

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, LegacyAlgorithm, expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }
}