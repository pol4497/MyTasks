using MyTasks.Models;

namespace MyTasks.Repositories
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<TaskItem>> GetTasksAsync();
        Task<TaskItem?> GetTaskByIdAsync(int id);
        void AddTask(TaskItem task);
        void UpdateTask(TaskItem task);
        void DeleteTask(TaskItem task);
        bool TaskExists(int id);
        Task<bool> SaveChangesAsync();
    }
}
