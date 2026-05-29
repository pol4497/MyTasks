using System.ComponentModel.DataAnnotations;
using TaskStatus = MyTasks.Models.TaskStatus;

namespace MyTasks.Dtos
{
    public record TaskCreateDto : IValidatableObject
    {
        [Required]
        [StringLength(200)]
        public string Title { get; init; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; init; } = string.Empty;

        public DateTime? DueDate { get; init; }

        [StringLength(100)]
        public string Category { get; init; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult("DueDate cannot be in the past.", new[] { nameof(DueDate) });
            }
        }
    }

    public record TaskUpdateDto : IValidatableObject
    {
        [Required]
        [StringLength(200)]
        public string Title { get; init; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; init; } = string.Empty;

        public DateTime? DueDate { get; init; }

        [StringLength(100)]
        public string Category { get; init; } = string.Empty;

        [Required]
        public TaskStatus Status { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult("DueDate cannot be in the past.", new[] { nameof(DueDate) });
            }
        }
    }

    public record TaskReadDto
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
        public string Category { get; init; } = string.Empty;
        public string Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    public class TaskItemDtos
    {
        public TaskStatus? Status { get; set; }
        public string? Category { get; set; }

        // Due date filters
        public DateTime? DueBefore { get; set; }
        public DateTime? DueAfter { get; set; }

        public string? Search { get; set; }

        // Sorting: Title | DueDate | Status (default = DueDate)
        public string SortBy { get; set; } = "DueDate";

        // true = descending
        public bool Desc { get; set; } = false;

        // Pagination
        [Range(1, 100)]
        public int? Limit { get; set; }
        [Range(1, int.MaxValue)]
        public int? Offset { get; set; }
    }
}
