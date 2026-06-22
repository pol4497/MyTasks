using MyTasks.Dtos;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Defines a contract for task data access operations.
    /// </summary>
    public interface ITaskRepository
    {
        /// <summary>
        /// Retrieves tasks using filtering, sorting, and pagination options.
        /// </summary>
        Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskItemDtos queryParams);

        /// <summary>
        /// Retrieves a task by its unique identifier.
        /// </summary>
        Task<TaskItem?> GetTaskByIdAsync(int id);

        /// <summary>
        /// Adds a new task to the collection.
        /// </summary>
        void AddTask(TaskItem task);

        /// <summary>
        /// Deletes the specified task from the system.
        /// </summary>
        void DeleteTask(TaskItem task);

        /// <summary>
        /// Determines whether a task with the specified identifier exists in the collection.
        /// </summary>
        bool TaskExists(int id);

        /// <summary>
        /// Asynchronously saves all changes made in the current context to the underlying database.
        /// </summary>
        Task<bool> SaveChangesAsync();
    }
}
