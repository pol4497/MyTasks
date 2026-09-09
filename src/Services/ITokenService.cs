using MyTasks.Models;

namespace MyTasks.Services
{
    public record GeneratedToken(string Value, DateTime ExpiresAt);

    /// <summary>
    /// Creates and hashes the tokens used for authentication.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Creates a short-lived signed JWT access token for the given user.
        /// </summary>
        GeneratedToken GenerateAccessToken(User user);

        /// <summary>
        /// Creates a long-lived, cryptographically random refresh token (raw, unhashed).
        /// </summary>
        GeneratedToken GenerateRefreshToken();

        /// <summary>
        /// Hashes a raw refresh token for safe storage/lookup in the database.
        /// </summary>
        string HashRefreshToken(string rawToken);
    }
}