namespace MyTasks.Services
{
    public interface ITokenCleanupService
    {
        Task<int> CleanupAsync(CancellationToken cancellationToken);
    }
}
