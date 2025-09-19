using Microsoft.EntityFrameworkCore;

namespace MyTasks.Data
{
    public class MyTasksContext : DbContext
    {
        public MyTasksContext(DbContextOptions<MyTasksContext> options)
            : base(options)
        {
        }
        public DbSet<MyTasks.Models.TaskItem> TaskItems { get; set; } = default!;
    }
}
