using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Dtos;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Implementation of task data access operations.
    /// </summary>
    public class TaskRepository(MyTasksContext _context) : ITaskRepository
    {
        /// <summary>
        /// Retrieves tasks using optional filtering, sorting, and pagination.
        /// </summary>
        public async Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskItemDtos queryParams)
        {
            IQueryable<TaskItem> query = _context.TaskItems.AsNoTracking();

            if (queryParams == null)
            {
                return await query.ToListAsync();
            }

            if (queryParams.Status.HasValue)
            {
                query = query.Where(t => t.Status == (Models.TaskStatus)queryParams.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.Category))
            {
                query = query.Where(t =>
                    EF.Functions.Collate(t.Category, "NOCASE") == queryParams.Category);
            }

            if (queryParams.DueBefore.HasValue)
            {
                var before = queryParams.DueBefore.Value;
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= before);
            }

            if (queryParams.DueAfter.HasValue)
            {
                var after = queryParams.DueAfter.Value;
                query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= after);
            }

            if (!string.IsNullOrWhiteSpace(queryParams.Search))
            {
                var s = queryParams.Search.Trim();
                query = query.Where(t =>
                    EF.Functions.Like(t.Title, $"%{s}%") ||
                    EF.Functions.Like(t.Description, $"%{s}%"));
            }

            var sortBy = queryParams.SortBy?.Trim().ToLowerInvariant();
            query = sortBy switch
            {
                "title" => queryParams.Desc ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
                "status" => queryParams.Desc ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
                // default to due date
                _ => queryParams.Desc ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            };

            if (queryParams.Offset.HasValue && queryParams.Offset.Value > 0) query = query.Skip(queryParams.Offset.Value);
            if (queryParams.Limit.HasValue && queryParams.Limit.Value > 0) query = query.Take(queryParams.Limit.Value);

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves a task by its unique identifier.
        /// Returns null if not found.
        /// </summary>
        public async Task<TaskItem?> GetTaskByIdAsync(int id)
        {
            return await _context.TaskItems.FindAsync(id);
        }

        public void AddTask(TaskItem task)
        {
            _context.Add(task);
        }
        
        public void DeleteTask(TaskItem task)
        {
            _context.TaskItems.Remove(task);
        }

        public bool TaskExists(int id)
        {
            return _context.TaskItems.Any(x =>  x.Id == id);
        }

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
