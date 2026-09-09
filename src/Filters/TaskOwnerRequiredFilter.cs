using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyTasks.Contexts;

namespace MyTasks.Filters
{
    /// <summary>
    /// Ensures the request has a resolved task owner (an authenticated user, or a valid
    /// guest session) before the action runs. TaskOwnerMiddleware resolves ownership for
    /// every request but never blocks on its own - registration and login must stay
    /// reachable by callers with neither a token nor a guest session yet. This filter is
    /// the actual enforcement point, applied only where ownership is mandatory.
    /// </summary>
    public class TaskOwnerRequiredFilter(ITaskOwnerContext _ownerContext) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!_ownerContext.IsAuthenticatedUser && !_ownerContext.IsGuest)
            {
                context.Result = new ObjectResult(new
                {
                    title = "Unauthorized",
                    detail = "Provide a valid Bearer token or an X-Guest-Token header."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            await next();
        }
    }
}
