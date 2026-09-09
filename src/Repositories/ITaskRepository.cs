using MyTasks.Dtos;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskItemDtos queryParams, int? userId, int? guestSessionId);

        Task<TaskItem?> GetTaskByIdAsync(int id, int? userId, int? guestSessionId);

        void AddTask(TaskItem task);

        void DeleteTask(TaskItem task);

        /// <summary>
        /// Reassigns every task owned by the given guest session to a user instead - used
        /// when a guest registers or logs in, so they don't lose what they created as a guest.
        /// Returns the number of tasks reassigned.
        /// </summary>
        Task<int> ClaimGuestTasksAsync(int guestSessionId, int userId);

        Task<bool> SaveChangesAsync();
    }
}
