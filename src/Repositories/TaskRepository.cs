using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Dtos;
using MyTasks.Exceptions;
using MyTasks.Models;

namespace MyTasks.Repositories
{
    /// <summary>
    /// Implementation of task data access operations.
    /// </summary>
    public class TaskRepository(MyTasksContext context) : RepositoryBase(context), ITaskRepository
    {
        /// <summary>
        /// Scopes a task query to whichever owner applies (a user or a guest session).
        /// Neither set means the caller has no resolved owner - returns an empty query
        /// rather than an error, since callers already enforce that ownership is required
        /// before reaching the repository (see TaskOwnerRequiredFilter).
        /// </summary>
        private IQueryable<TaskItem> ScopedToOwner(int? userId, int? guestSessionId)
        {
            IQueryable<TaskItem> query = _context.TaskItems;

            return userId.HasValue
                ? query.Where(t => t.UserId == userId.Value)
                : guestSessionId.HasValue
                    ? query.Where(t => t.GuestSessionId == guestSessionId.Value)
                    : query.Where(_ => false);
        }

        public async Task<IReadOnlyList<TaskItem>> GetTasksAsync(TaskItemDtos queryParams, int? userId, int? guestSessionId)
        {
            IQueryable<TaskItem> query = ScopedToOwner(userId, guestSessionId).AsNoTracking();

            if (queryParams == null)
            {
                return await query.ToListAsync();
            }

            if (queryParams.Status.HasValue)
            {
                query = query.Where(t => t.Status == queryParams.Status.Value);
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

            var sortBy = queryParams.SortBy?.Trim();

            if (!string.IsNullOrWhiteSpace(sortBy) &&
                !sortBy.Equals("Title", StringComparison.OrdinalIgnoreCase) &&
                !sortBy.Equals("Category", StringComparison.OrdinalIgnoreCase) &&
                !sortBy.Equals("Status", StringComparison.OrdinalIgnoreCase) &&
                !sortBy.Equals("DueDate", StringComparison.OrdinalIgnoreCase))  
            {
                throw new BadRequestException("Invalid sort field.");
            }

            query = sortBy?.ToLowerInvariant() switch
            {
                "title" => queryParams.Desc 
                    ? query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id)
                    : query.OrderBy(t => t.Title).ThenBy(t => t.Id),

                "category" => queryParams.Desc 
                    ? query.OrderByDescending(t => t.Category).ThenByDescending(t => t.Id)
                    : query.OrderBy(t => t.Category).ThenBy(t => t.Id),
                "status" => queryParams.Desc 
                    ? query.OrderByDescending(t => t.Status).ThenByDescending(t => t.Id)
                    : query.OrderBy(t => t.Status).ThenBy(t => t.Id),

                _ => queryParams.Desc 
                    ? query.OrderByDescending(t => t.DueDate).ThenByDescending(t => t.Id)
                    : query.OrderBy(t => t.DueDate).ThenBy(t => t.Id),
            };

            if (queryParams.Offset.HasValue && queryParams.Offset.Value > 0) query = query.Skip(queryParams.Offset.Value);
            if (queryParams.Limit.HasValue && queryParams.Limit.Value > 0) query = query.Take(queryParams.Limit.Value);

            return await query.ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id, int? userId, int? guestSessionId)
        {
            return await ScopedToOwner(userId, guestSessionId)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public void AddTask(TaskItem task)
        {
            _context.Add(task);
        }
        
        public void DeleteTask(TaskItem task)
        {
            _context.TaskItems.Remove(task);
        }

        public async Task<int> ClaimGuestTasksAsync(int guestSessionId, int userId)
        {
            return await _context.TaskItems
                .Where(t => t.GuestSessionId == guestSessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.UserId, userId)
                    .SetProperty(t => t.GuestSessionId, (int?)null));
        }
    }
}
