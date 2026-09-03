using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyTasks.Data;

namespace MyTasks.IntegrationTests.Infrastructure;

public class MyTasksWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public MyTasksWebApplicationFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "Jwt:Key",
            "integration-test-jwt-key-that-is-long-enough-123456789");

        builder.UseSetting(
            "Jwt:Issuer",
            "MyTasks.IntegrationTests");

        builder.UseSetting(
            "Jwt:Audience",
            "MyTasks.IntegrationTests");

        builder.ConfigureServices(services =>
        {
            // Remove the application's MyTasksContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MyTasksContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Register an isolated in-memory SQLite database.
            services.AddDbContext<MyTasksContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Create the database schema.
            using var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<MyTasksContext>();

            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}