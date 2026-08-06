using Microsoft.EntityFrameworkCore;
using MyTasks.Models;

namespace MyTasks.Data
{
    public class MyTasksContext : DbContext
    {
        public MyTasksContext(DbContextOptions<MyTasksContext> options)
            : base(options)
        {
        }
        public DbSet<TaskItem> TaskItems { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(user => user.Username).IsUnique();
                entity.HasIndex(user => user.Email).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(token => token.TokenHash).IsUnique();

                entity.HasOne(token => token.User)
                    .WithMany(user => user.RefreshTokens)
                    .HasForeignKey(user => user.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}