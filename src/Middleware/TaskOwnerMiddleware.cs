using MyTasks.Services;

namespace MyTasks.Middleware
{
    public class TaskOwnerMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(
        HttpContext _context,
        ITaskOwnerResolver _ownerResolver)
        {
            await _ownerResolver.ResolveAsync();

            await next(_context);
        }
    }
}
