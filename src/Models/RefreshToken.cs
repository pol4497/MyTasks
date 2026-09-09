namespace MyTasks.Models
{
    /// <summary>
    /// Represents an active or past login session. A new row is created every time a user
    /// logs in, and it's revoked on logout or when it expires.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }

        /// <summary>
        /// SHA-256 hash of the refresh token. The raw token is only ever handed to the
        /// client and is never stored, so a database leak alone can't be used to log in.
        /// </summary>
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
