namespace MyTasks.Exceptions
{
    public class NotFoundException(string message) : AppException(message)
    {
    }
}
