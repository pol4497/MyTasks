using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    public class GuestSessionRepository(MyTasksContext _context) : IGuestSessionRepository
    {
        public async Task<GuestSession?> GetByTokenHashAsync(string tokenHash)
        {
            return await _context.GuestSessions
                .FirstOrDefaultAsync(session => session.TokenHash == tokenHash);
        }

        public void Add(GuestSession session)
        {
            _context.GuestSessions.Add(session);
        }

        public async Task InvalidateAsync(int guestSessionId)
        {
            var session = await _context.GuestSessions.FindAsync(guestSessionId);
            if (session == null) return;

            session.ExpiresAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}