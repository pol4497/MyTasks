namespace MyTasks.Models
{
    public enum TaskStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Exactly one owner is set for every newly created task:
        // either an authenticated user or an anonymous guest session.
        public int? UserId { get; set; }
        public User? User { get; set; }

        public int? GuestSessionId { get; set; }
        public GuestSession? GuestSession { get; set; }
    }
}
