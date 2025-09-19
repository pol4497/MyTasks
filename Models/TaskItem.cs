namespace MyTasks.Models
{
    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
    }
}
