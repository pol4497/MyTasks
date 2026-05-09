using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    public class TaskRepository(MyTasksContext _context) : ITaskRepository
    {
        public async Task<IReadOnlyList<TaskItem>> GetTasksAsync()
        {
            return await _context.TaskItems.ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _context.TaskItems.FindAsync(id);
        }

        public void AddTask(TaskItem task)
        {
            _context.Add(task);
        }

        public void UpdateTask(TaskItem task)
        {
            _context.Entry(task).State = EntityState.Modified;
        }

        public void DeleteTask(TaskItem task)
        {
            _context.TaskItems.Remove(task);
        }

        public bool TaskExists(int id)
        {
            return _context.TaskItems.Any(x =>  x.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        
    }
}
