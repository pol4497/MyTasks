namespace MyTasks.Repositories
{
    public interface IUnitOfWork
    {
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
