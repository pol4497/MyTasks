using MyTasks.Dtos;
using MyTasks.Models;
using MyTasks.Repositories;

namespace MyTasks.Services
{
    public class GuestSessionService(
        IGuestSessionRepository _guestSessions,
        IGuestTokenService _guestTokens,
        IConfiguration _config) : IGuestSessionService
    {
        public async Task<GuestSessionResponseDto> CreateGuestSessionAsync()
        {
            var days = _config.GetValue<int?>("GuestSession:ExpiresAfterDays") ?? 30;

            var rawToken = _guestTokens.Generate();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddDays(days);

            var session = new GuestSession
            {
                TokenHash = _guestTokens.Hash(rawToken),
                CreatedAt = now,
                LastAccessedAt = now,
                ExpiresAt = expiresAt
            };

            _guestSessions.Add(session);
            await _guestSessions.SaveChangesAsync();

            return new GuestSessionResponseDto
            {
                GuestToken = rawToken,
                ExpiresAt = expiresAt
            };
        }
    }
}
