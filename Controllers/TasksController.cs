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

        // GET: api/Tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            var items = await _context.TaskItems.AsNoTracking().ToListAsync();
            return Ok(items);
        }

        // GET: api/Tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var item = await _context.TaskItems.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            if (taskItem == null) return BadRequest();
            _context.TaskItems.Add(taskItem);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
        }

        // PUT: api/Tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem taskItem)
        {
            if (taskItem == null || id != taskItem.Id) return BadRequest();

            var existing = await _context.TaskItems.FindAsync(id);
            if (existing == null) return NotFound();

            // map allowed fields
            existing.Title = taskItem.Title;
            existing.Description = taskItem.Description;
            existing.DueDate = taskItem.DueDate;
            existing.Category = taskItem.Category;
            existing.Status = taskItem.Status;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var existing = await _context.TaskItems.FindAsync(id);
            if (existing == null) return NotFound();

            _context.TaskItems.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
