using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    public class GuestSessionRepository(MyTasksContext context) : RepositoryBase(context), IGuestSessionRepository
    {
        public async Task<GuestSession?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.GuestSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(session => session.TokenHash == tokenHash);
        }

        public void Add(GuestSession session)
        {
            _context.GuestSessions.Add(session);
        }

        public async Task TouchAsync(int guestSessionId, DateTime now)
        {
            await _context.GuestSessions
                .Where(session => session.Id == guestSessionId && session.ExpiresAt > now)
                .ExecuteUpdateAsync(update => update.SetProperty(session => session.LastAccessedAt, now));
        }

        public async Task<bool> TryConsumeAsync(int guestSessionId, DateTime now)
        {
            var rowsAffected = await _context.GuestSessions
                .Where(session => session.Id == guestSessionId && session.ExpiresAt > now)
                .ExecuteUpdateAsync(update => update.SetProperty(session => session.ExpiresAt, now));

            return rowsAffected == 1;
        }
    }
}