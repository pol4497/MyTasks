using Microsoft.AspNetCore.Mvc;
using MyTasks.Models;
using MyTasks.Repositories;

namespace MyTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController(ITaskRepository _repo) : ControllerBase
    {

        // GET: api/Tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return Ok(await _repo.GetTasksAsync());
        }

        // GET: api/Tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _repo.GetTaskByIdAsync(id);

            if (task == null) return NotFound();
            return task;
        }

        // POST: api/Tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem taskItem)
        {
            _repo.AddTask(taskItem);
            if (await _repo.SaveChangesAsync())
            {
                return CreatedAtAction(nameof(GetTask), new { id = taskItem.Id }, taskItem);
            }
            return BadRequest("Problem creating this Task");
        }

        // PUT: api/Tasks/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem taskItem)
        {
            if (taskItem.Id != id || !_repo.TaskExists(id))
                return BadRequest("Cannot update this task");

            _repo.UpdateTask(taskItem);

            if (await _repo.SaveChangesAsync())
            {
                return NoContent();
            }

            return BadRequest("Problem updating this task");
        }

        // DELETE: api/Tasks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _repo.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            _repo.DeleteTask(task);
            if (await _repo.SaveChangesAsync())
            {
                return NoContent();
            }

            return BadRequest("Problem deleting this task");
        }
    }
}
