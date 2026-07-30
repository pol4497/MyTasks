using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MyTasks.Middleware
{
    internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, 
        ILogger<GlobalExceptionHandler> logger, 
        IHostEnvironment env) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception ex, CancellationToken cancellationToken)
        {
            logger.LogError(ex, "Unhandled exception occurred");

            var isDevelopment = env.IsDevelopment();

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
                    Detail = isDevelopment ? ex.Message : "Unhandled exception occurred"
                }
            });
        }
    }
}
