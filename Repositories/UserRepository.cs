using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Implementation of user and login-session data access operations.
    /// </summary>
    public class UserRepository(MyTasksContext context) : RepositoryBase(context), IUserRepository
    {
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(user => user.Username == username);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users
                .AnyAsync(user => user.Username == username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(user => user.Email == email);
        }

        public async Task<bool> AnyAdminExistsAsync()
        {
            return await _context.Users.AnyAsync(u => u.Role == UserRole.Admin);
        }

        public void AddUser(User user)
        {
            _context.Users.Add(user);
        }

        public async Task<IReadOnlyList<User>> GetAllUsersAsync()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public void AddRefreshToken(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
        }

        public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(token => token.User)
                .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
        }

        public async Task<bool> TryConsumeRefreshTokenAsync(int tokenId, DateTime now)
        {
            var rowsAffected = await _context.RefreshTokens
                .Where(t => t.Id == tokenId && 
                t.RevokedAt == null &&
                t.ExpiresAt > now)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

            return rowsAffected == 1;
        }
    }
}
