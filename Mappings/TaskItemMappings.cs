using MyTasks.Dtos;
using MyTasks.Models;

namespace MyTasks.Mappings
{
    public static class TaskItemMappings
    {
        public static TaskReadDto ToReadDto(this TaskItem task)
        {
            return new TaskReadDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Category = task.Category,
                Status = task.Status
            };
        }

        public static TaskItem ToEntity(this TaskCreateDto dto)
        {
            return new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Category = dto.Category,
                Status = Models.TaskStatus.Pending
            };
        }

        public static void UpdateEntity(this TaskUpdateDto dto, TaskItem task)
        {
            task.Title = dto.Title;
            task.Description = dto.Description;
            task.DueDate = dto.DueDate;
            task.Category = dto.Category;
            task.Status = dto.Status;
        }
    }
}
