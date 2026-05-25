using Microsoft.AspNetCore.Mvc;
using MyTasks.Dtos;
using MyTasks.Mappings;
using MyTasks.Models;
using MyTasks.Repositories;

namespace MyTasks.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController(ITaskRepository _repo) : ControllerBase
    {

        /// <summary>
        /// Retrieves a collection of tasks based on the specified query parameters.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TaskReadDto>>> GetTasks([FromQuery] TaskItemDtos queryParams)
        {
            var tasks = await _repo.GetTasksAsync(queryParams);
            var dtos = tasks.Select(t => t.ToReadDto());
            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a task by its unique identifier.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskReadDto>> GetTask(int id)
        {
            var task = await _repo.GetTaskByIdAsync(id);

            if (task == null) return NotFound();
            return Ok(task.ToReadDto());
        }

        /// <summary>
        /// Creates a new task and saves it to the repository.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(TaskReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskReadDto>> CreateTask([FromBody] TaskCreateDto dto)
        {
            var task = dto.ToEntity();
            _repo.AddTask(task);
            await _repo.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task.ToReadDto());
        }

        /// <summary>
        /// Updates an existing task with the specified ID using the provided data.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto dto)
        {
            var existingTask = await _repo.GetTaskByIdAsync(id);

            if (existingTask == null)
            {
                return NotFound();
            }

            dto.UpdateEntity(existingTask);

            await _repo.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Deletes the task with the specified identifier.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _repo.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            _repo.DeleteTask(task);
            await _repo.SaveChangesAsync();
            return NoContent();
        }
    }
}
