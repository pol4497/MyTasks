namespace MyTasks.Services
{
    public interface IGuestTokenService
    {
        string Generate();
        string Hash(string rawToken);
    }
}
