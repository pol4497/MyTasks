namespace MyTasks.Services
{
    /// <summary>
    /// Performs periodic cleanup of expired and revoked refresh tokens.
    /// </summary>
    /// <remarks>
    /// The cleanup runs once every hour while the application is running.
    /// </remarks>
    public class TokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TokenCleanupBackgroundService> logger
        ) : BackgroundService
    {
        /// <summary>
        /// Starts the background cleanup process.
        /// </summary>
        /// <param name="stoppingToken">
        /// A token that signals when the application is shutting down.
        /// </param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = scopeFactory.CreateScope();

                var cleanupService =
                    scope.ServiceProvider
                        .GetRequiredService<ITokenCleanupService>();

                var deleted = await cleanupService.CleanupAsync(
                    stoppingToken);

                logger.LogInformation(
                    "Deleted {Count} old refresh tokens.",
                    deleted);
            }
        }
    }
}
