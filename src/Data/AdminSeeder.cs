using MyTasks.Models;
using MyTasks.Repositories;
using MyTasks.Security;

namespace MyTasks.Data
{
    /// <summary>
    /// Creates the first Admin user from configuration, once, on startup - so there's
    /// no need to hand-edit the database or expose an HTTP endpoint for it.
    /// Reads the "AdminSeed" configuration section:
    /// {
    ///   "AdminSeed": { "Username": "...", "Email": "...", "Password": "..." }
    /// }
    /// Put real values in user secrets / environment variables, not in source control.
    /// </summary>
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            var users = services.GetRequiredService<IUserRepository>();

            // Already have an Admin - nothing to do. Keeps this safe to run on every startup.
            if (await users.AnyAdminExistsAsync())
            {
                return;
            }

            var section = config.GetSection("AdminSeed");
            var username = section["Username"];
            var email = section["Email"];
            var password = section["Password"];

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
                logger.LogWarning(
                    "No Admin user exists and AdminSeed configuration is incomplete - skipping admin seeding. " +
                    "Set AdminSeed:Username, AdminSeed:Email, and AdminSeed:Password (e.g. via user secrets) to seed one.");
                return;
            }

            var admin = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            users.AddUser(admin);
            await users.SaveChangesAsync();
        }
    }
}
