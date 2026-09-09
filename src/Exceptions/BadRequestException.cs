namespace MyTasks.Exceptions
{
    public class BadRequestException(string message) : AppException(message)
    {
    }
}
