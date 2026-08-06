namespace MyTasks.Exceptions
{
    /// <summary>
    /// Thrown when a request conflicts with existing state (e.g. a username/email already taken).
    /// Mapped to HTTP 409 by <see cref="MyTasks.Middleware.GlobalExceptionHandler"/>.
    /// </summary>
    public class ConflictException(string message) : Exception(message)
    {
    }
}
