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
        Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskItemDtos queryParams, int? userId, int? guestSessionId);

        /// <summary>
        /// Retrieves a task by its unique identifier.
        /// </summary>
        Task<TaskItem?> GetTaskByIdAsync(int id, int? userId, int? guestSessionId);

        /// <summary>
        /// Adds a new task to the collection.
        /// </summary>
        void AddTask(TaskItem task);

        /// <summary>
        /// Deletes the specified task from the system.
        /// </summary>
        void DeleteTask(TaskItem task);

        /// <summary>
        /// Reassigns every task owned by the given guest session to a user instead - used
        /// when a guest registers or logs in, so they don't lose what they created as a guest.
        /// Returns the number of tasks reassigned.
        /// </summary>
        Task<int> ClaimGuestTasksAsync(int guestSessionId, int userId);

        /// <summary>
        /// Asynchronously saves all changes made in the current context to the underlying database.
        /// </summary>
        Task<bool> SaveChangesAsync();
    }
}
