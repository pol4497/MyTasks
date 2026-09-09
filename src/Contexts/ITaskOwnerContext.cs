namespace MyTasks.Contexts
{
    public interface ITaskOwnerContext
    {
        int? UserId { get; }
        int? GuestSessionId { get; }

        bool IsAuthenticatedUser { get; }
        bool IsGuest { get; }
        public void SetUser(int userId);
        public void SetGuest(int guestSessionId);
    }
}
