using Microsoft.EntityFrameworkCore;
using MyTasks.Data;

namespace MyTasks.Services
{
    /// <summary>
    /// Provides operations for cleaning up old refresh tokens.
    /// </summary>
    public class TokenCleanupService(MyTasksContext context) : ITokenCleanupService
    {
        /// <summary>
        /// Deletes refresh tokens that have been expired or revoked
        /// for more than seven days.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token that can be used to cancel the cleanup operation.
        /// </param>
        /// <returns>
        /// The number of refresh tokens that were deleted.
        /// </returns>
        public async Task<int> CleanupAsync(
            CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            return await context.RefreshTokens
                .Where(x =>
                    x.ExpiresAt < cutoff ||
                    x.RevokedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
