using MyTasks.Contexts;
using MyTasks.Repositories;
using System.Security.Claims;

namespace MyTasks.Services
{
    public class TaskOwnerResolver(
    IHttpContextAccessor httpContextAccessor,
    IGuestSessionRepository guestSessions,
    IGuestTokenService guestTokens,
    ITaskOwnerContext ownerContext)
    : ITaskOwnerResolver
    {
        private const string GuestTokenHeader = "X-Guest-Token";

        public async Task<bool> ResolveAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext == null)
                return false;

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var claim = httpContext.User
                    .FindFirst(ClaimTypes.NameIdentifier);

                if (!int.TryParse(claim?.Value, out var userId))
                    return false;

                ownerContext.SetUser(userId);

                return true;
            }

            var rawToken = httpContext.Request.Headers
                .TryGetValue(GuestTokenHeader, out var token)
                    ? token.FirstOrDefault()
                    : null;

            if (string.IsNullOrWhiteSpace(rawToken))
                return false;

            var hash = guestTokens.Hash(rawToken);

            var session =
                await guestSessions.GetByTokenHashAsync(hash);

            if (session == null || !session.IsActive)
                return false;

            session.LastAccessedAt = DateTime.UtcNow;
            await guestSessions.SaveChangesAsync();

            ownerContext.SetGuest(session.Id);

            return true;
        }
    }
}
