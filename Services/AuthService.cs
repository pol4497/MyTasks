using MyTasks.Contexts;
using MyTasks.Dtos;
using MyTasks.Exceptions;
using MyTasks.Mappings;
using MyTasks.Models;
using MyTasks.Repositories;
using MyTasks.Security;

namespace MyTasks.Services
{
    public class AuthService(
        IUserRepository _users,
        ITokenService _tokens,
        ITaskRepository _tasks,
        IGuestSessionRepository _guestSessions,
        ITaskOwnerContext _ownerContext
        ) : IAuthService
    {
        private async Task<AuthResponseDto> IssueTokensAsync(User user)
        {
            var accessToken = _tokens.GenerateAccessToken(user);
            var refreshToken = _tokens.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                TokenHash = _tokens.HashRefreshToken(refreshToken.Value),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = refreshToken.ExpiresAt,
                UserId = user.Id
            };

            _users.AddRefreshToken(refreshTokenEntity);
            await _users.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken.Value,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshToken = refreshToken.Value,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = user.ToReadDto()
            };
        }

        /// <summary>
        /// If the current request is presenting a valid guest session, transfers that
        /// session's tasks to the given (newly registered or just-logged-in) user, then
        /// invalidates the guest session so a leftover X-Guest-Token can't reuse it.
        /// </summary>
        private async Task ClaimGuestTasksIfAnyAsync(int userId)
        {
            if (!_ownerContext.IsGuest) return;

            var guestSessionId = _ownerContext.GuestSessionId!.Value;

            await _tasks.ClaimGuestTasksAsync(guestSessionId, userId);
            await _guestSessions.InvalidateAsync(guestSessionId);
        }

        public async Task<UserReadDto> RegisterAsync(RegisterDto dto)
        {
            if (await _users.UsernameExistsAsync(dto.Username))
            {
                throw new ConflictException("Username is already taken.");
            }

            if (await _users.EmailExistsAsync(dto.Email))
            {
                throw new ConflictException("Email is already registered.");
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            _users.AddUser(user);
            await _users.SaveChangesAsync();

            await ClaimGuestTasksIfAnyAsync(user.Id);

            return user.ToReadDto();
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _users.GetByUsernameAsync(dto.Username);

            if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password.");
            }

            await ClaimGuestTasksIfAnyAsync(user.Id);

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResponseDto> RefreshAsync(string rawRefreshToken)
        {
            var hash = _tokens.HashRefreshToken(rawRefreshToken);
            var existingToken = await _users.GetRefreshTokenByHashAsync(hash);

            if (existingToken == null || !existingToken.IsActive)
            {
                throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
            }

            // Rotate: revoke the used token and issue a brand new pair.
            existingToken.RevokedAt = DateTime.UtcNow;

            return await IssueTokensAsync(existingToken.User);
        }

        public async Task LogoutAsync(string rawRefreshToken)
        {
            var hash = _tokens.HashRefreshToken(rawRefreshToken);
            var existingToken = await _users.GetRefreshTokenByHashAsync(hash);

            if (existingToken == null || !existingToken.IsActive)
            {
                // Already logged out / unknown token - logout is idempotent, nothing to do.
                return;
            }

            existingToken.RevokedAt = DateTime.UtcNow;
            await _users.SaveChangesAsync();
        }
    }
}