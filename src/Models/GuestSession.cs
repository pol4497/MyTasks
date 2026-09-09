namespace MyTasks.Models
{
    /// <summary>
    /// Represents an anonymous task owner. The raw token is never stored; only its SHA-256 hash is persisted.
    /// </summary>
    public class GuestSession
    {
        public int Id { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        public bool IsActive => ExpiresAt > DateTime.UtcNow;
    }
}