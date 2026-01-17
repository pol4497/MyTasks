using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MyTasks.Exceptions
{
    internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception ex, CancellationToken cancellationToken)
        {
            logger.LogError(ex, "Unhandled exception occured");

            httpContext.Response.StatusCode = ex switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                ArgumentException or InvalidOperationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = ex,
                ProblemDetails = new ProblemDetails
                {
                    Status = httpContext.Response.StatusCode,
                    Type = ex.GetType().Name,
                    Title = ex switch
                    {
                        KeyNotFoundException => "Resource Not Found",
                        ArgumentException or InvalidOperationException => "Bad Request",
                        _ => "Internal Server Error"
                    },
                    Detail = ex.Message,
                }
            });
        }
    }
}
