using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTasks.Models;

namespace MyTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly Data.MyTasksContext _context;

        public TasksController(Data.MyTasksContext context)
        {
            _context = context;
        }

        private static readonly object _tasksLock = new object();

        static private List<TaskItem> tasks = new List<TaskItem>()
        {
            new TaskItem()
            {
                Id = 1,
                Title = "Buy groceries",
                Description = "Milk, eggs, bread",
                DueDate = DateTime.UtcNow.AddDays(1),
                Category = "Personal",
                Status = Models.TaskStatus.Pending
            },
            new TaskItem()
            {
                Id = 2,
                Title = "Finish project report",
                Description = "Complete the final draft of the project report",
                DueDate = DateTime.UtcNow.AddDays(3),
                Category = "Work",
                Status = Models.TaskStatus.InProgress
            },
            new TaskItem()
            {
                Id = 3,
                Title = "Call plumber",
                Description = "Fix the leaking sink in the kitchen",
                DueDate = DateTime.UtcNow.AddDays(2),
                Category = "Home",
                Status = Models.TaskStatus.Pending
            },
            new TaskItem()
            {
                Id = 4,
                Title = "Schedule dentist appointment",
                Description = "Routine check-up and cleaning",
                DueDate = DateTime.UtcNow.AddDays(7),
                Category = "Health",
                Status = Models.TaskStatus.Pending
            }
        };

        [HttpGet]
        public Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            List<TaskItem> copy;
            lock(_tasksLock)
            {
                copy = tasks.ToList();
            }
            return Task.FromResult<ActionResult<IEnumerable<TaskItem>>>(Ok(copy));
        }

        [HttpGet("{id}")]
        public Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var item = tasks.FirstOrDefault(t => t.Id == id);
            if (item == null) return Task.FromResult<ActionResult<TaskItem>>(NotFound());
            return Task.FromResult<ActionResult<TaskItem>>(item);
        }

        [HttpPost]
        public Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            if (taskItem == null) return Task.FromResult<ActionResult<TaskItem>>(BadRequest());

            lock (_tasksLock)
            {
                tasks.Add(taskItem);
            }

            return Task.FromResult<ActionResult<TaskItem>>(CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> UpdateTask(int id, TaskItem taskItem)
        {
            if (taskItem == null || id != taskItem.Id) return Task.FromResult<IActionResult>(BadRequest());

            lock (_tasksLock)
            {
                var existing = tasks.FirstOrDefault(t => t.Id == id);
                if (existing == null) return Task.FromResult<IActionResult>(NotFound());

                // map allowed fields
                existing.Title = taskItem.Title;
                existing.Description = taskItem.Description;
                existing.DueDate = taskItem.DueDate;
                existing.Category = taskItem.Category;
                existing.Status = taskItem.Status;
            }

            return Task.FromResult<IActionResult>(NoContent());
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteTask(int id)
        {
            lock (_tasksLock)
            {
                var existing = tasks.FirstOrDefault(t => t.Id == id);
                if (existing == null) return Task.FromResult<IActionResult>(NotFound());
                tasks.Remove(existing);
            }

            return Task.FromResult<IActionResult>(NoContent());
        }
    }
}
