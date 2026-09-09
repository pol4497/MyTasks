namespace MyTasks.Services
{
    public interface ITaskOwnerResolver
    {
        Task<bool> ResolveAsync();
    }
}
