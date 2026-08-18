using MyTasks.Models;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Defines a contract for user and login-session data access operations.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> AnyAdminExistsAsync();
        void AddUser(User user);
        Task<IReadOnlyList<User>> GetAllUsersAsync();

        void AddRefreshToken(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash);

        /// <summary>
        /// Atomically consumes a refresh token, but only if it's still active. Returns false if
        /// it was already revoked or expired by the time this runs - which means a concurrent
        /// request already claimed it, so this caller must not proceed with rotation.
        /// </summary>
        Task<bool> TryConsumeRefreshTokenAsync(int tokenId, DateTime now);

        Task<bool> SaveChangesAsync();
    }
}
