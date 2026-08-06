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

        Task<bool> SaveChangesAsync();
    }
}
