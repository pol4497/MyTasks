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
        public DbSet<GuestSession> GuestSessions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(user => user.Username).IsUnique();
                entity.HasIndex(user => user.Email).IsUnique();
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasOne(task => task.User)
                    .WithMany(user => user.Tasks)
                    .HasForeignKey(task => task.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(task => task.GuestSession)
                    .WithMany(session => session.Tasks)
                    .HasForeignKey(task => task.GuestSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(task => task.UserId);
                entity.HasIndex(task => task.GuestSessionId);

                // Every task must have exactly one owner - never both, never neither.
                // Enforced here (not just in application code) so a bug elsewhere can't
                // silently create an orphaned or double-owned row.
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_TaskItem_OwnerXor",
                    "((UserId IS NOT NULL AND GuestSessionId IS NULL) OR (UserId IS NULL AND GuestSessionId IS NOT NULL))"));
            });

            modelBuilder.Entity<GuestSession>(entity =>
            {
                entity.HasIndex(session => session.TokenHash).IsUnique();
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(token => token.TokenHash).IsUnique();

                entity.HasOne(token => token.User)
                    .WithMany(user => user.RefreshTokens)
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}