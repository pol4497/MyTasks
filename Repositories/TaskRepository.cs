using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    public class TaskRepository(MyTasksContext context) : ITaskRepository
    {
        public async Task<IReadOnlyList<TaskItem>> GetTasksAsync()
        {
            return await context.TaskItems.ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await context.TaskItems.FindAsync(id);
        }

        public void AddTask(TaskItem task)
        {
            context.Add(task);
        }

        public void UpdateTask(TaskItem task)
        {
            context.Entry(task).State = EntityState.Modified;
        }

        public void DeleteTask(TaskItem task)
        {
            context.TaskItems.Remove(task);
        }

        public bool TaskExists(int id)
        {
            return context.TaskItems.Any(x =>  x.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        
    }
}
