namespace MyTasks.Services
{
    /// <summary>
    /// Runs a cleanup service on a fixed interval for the lifetime of the app. A fresh DI
    /// scope is created on every tick, since cleanup services are scoped (they depend on
    /// MyTasksContext) - the timer interval and log description are supplied per instance
    /// at registration, since DI can't inject plain values like these on its own.
    /// </summary>
    public class PeriodicCleanupBackgroundService<TCleanupService>(
        IServiceScopeFactory scopeFactory,
        ILogger<PeriodicCleanupBackgroundService<TCleanupService>> logger,
        TimeSpan interval,
        string itemDescription)
        : BackgroundService
        where TCleanupService : ICleanupService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = scopeFactory.CreateScope();

                var cleanupService =
                    scope.ServiceProvider
                        .GetRequiredService<TCleanupService>();

                var deleted = await cleanupService.CleanupAsync(stoppingToken);

                logger.LogInformation(
                    "Deleted {Count} {Item}.",
                    deleted, itemDescription);
            }
        }
    }
}