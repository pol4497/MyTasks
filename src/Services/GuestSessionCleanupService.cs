using Microsoft.EntityFrameworkCore;
using MyTasks.Data;

namespace MyTasks.Services
{
    /// <summary>
    /// Provides operations for cleaning up expired guest sessions.
    /// </summary>
    public class GuestSessionCleanupService(MyTasksContext context) : IGuestSessionCleanupService
    {
        /// <summary>
        /// Deletes guest sessions past their expiration. Unlike refresh token cleanup, this
        /// deletes immediately at expiry rather than after a grace period. TaskItem's foreign
        /// key to GuestSession cascades on delete, so any tasks an abandoned guest session
        /// left behind are removed along with it - no separate task cleanup needed.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token that can be used to cancel the cleanup operation.
        /// </param>
        /// <returns>
        /// The number of guest sessions that were deleted.
        /// </returns>
        public async Task<int> CleanupAsync(
            CancellationToken cancellationToken)
        {
            return await context.GuestSessions
                .Where(x => x.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}