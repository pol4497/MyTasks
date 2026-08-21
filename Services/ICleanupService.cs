namespace MyTasks.Services
{
    /// <summary>
    /// A service that can delete stale rows on demand and report how many it removed.
    /// Implemented by ITokenCleanupService and IGuestSessionCleanupService so both can be
    /// driven by the same generic PeriodicCleanupBackgroundService.
    /// </summary>
    public interface ICleanupService
    {
        Task<int> CleanupAsync(CancellationToken cancellationToken);
    }
}
