using MyTasks.Data;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Shared plumbing for repositories backed by MyTasksContext - just the context field
    /// and SaveChangesAsync, which were previously duplicated identically in every
    /// repository. Note that since all repositories share the same scoped DbContext
    /// instance per request, calling SaveChangesAsync on any one of them flushes every
    /// pending change tracked across all of them, not just its own.
    /// </summary>
    public abstract class RepositoryBase(MyTasksContext context)
    {
        protected readonly MyTasksContext _context = context;
 
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
