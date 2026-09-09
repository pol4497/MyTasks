namespace MyTasks.Contexts
{
    public class TaskOwnerContext : ITaskOwnerContext
    {
        public int? UserId { get; private set; }
        public int? GuestSessionId { get; private set; }

        public bool IsAuthenticatedUser => UserId.HasValue;
        public bool IsGuest => GuestSessionId.HasValue;

        public void SetUser(int userId)
        {
            UserId = userId;
            GuestSessionId = null;
        }

        public void SetGuest(int guestSessionId)
        {
            GuestSessionId = guestSessionId;
            UserId = null;
        }
    }
}
