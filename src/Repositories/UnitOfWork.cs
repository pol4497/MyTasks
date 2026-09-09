using MyTasks.Data;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Runs an application operation inside an EF Core database transaction.
    /// All repositories in the current scope share the same DbContext, so their
    /// changes participate in the same transaction.
    /// </summary>
    public class UnitOfWork(MyTasksContext context) : IUnitOfWork
    {
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
