using MyTasks.Models;

namespace MyTasks.Repositories
{
    public interface IGuestSessionRepository
    {
        Task<GuestSession?> GetByTokenHashAsync(string tokenHash);

        void Add(GuestSession session);

        /// <summary>
        /// Immediately expires a guest session (used once its tasks have been claimed by a
        /// registered/logged-in user, so a leftover X-Guest-Token can't keep resolving to it).
        /// </summary>
        Task InvalidateAsync(int guestSessionId);

        Task<bool> SaveChangesAsync();
    }
}