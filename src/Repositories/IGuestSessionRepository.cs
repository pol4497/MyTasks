using MyTasks.Models;

namespace MyTasks.Repositories
{
    public interface IGuestSessionRepository
    {
        Task<GuestSession?> GetByTokenHashAsync(string tokenHash);

        void Add(GuestSession session);

        /// <summary>
        /// Updates the last-accessed timestamp for an active guest session.
        /// This is intentionally separate from consuming the session during auth.
        /// </summary>
        Task TouchAsync(int guestSessionId, DateTime now);

        /// <summary>
        /// Atomically consumes an active guest session by expiring it. Returns false if
        /// the session was already expired/consumed by the time this runs, which means
        /// another request already claimed it.
        /// </summary>
        Task<bool> TryConsumeAsync(int guestSessionId, DateTime now);

        Task<bool> SaveChangesAsync();
    }
}