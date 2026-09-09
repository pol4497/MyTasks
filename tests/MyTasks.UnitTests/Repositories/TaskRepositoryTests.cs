using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Dtos;
using MyTasks.Models;
using MyTasks.Repositories;
using TaskStatus = MyTasks.Models.TaskStatus;

namespace MyTasks.UnitTests.Repositories;

public class TaskRepositoryTests
{
    [Fact]
    public async Task GetTasksAsync_UserOnlySeesOwnTasks()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user1 = await CreateUserAsync(db, "user1");
        var user2 = await CreateUserAsync(db, "user2");
        db.TaskItems.AddRange(Task("Mine", userId: user1.Id), Task("Theirs", userId: user2.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(new TaskItemDtos(), user1.Id, null);

        var task = Assert.Single(result);
        Assert.Equal("Mine", task.Title);
    }

    [Fact]
    public async Task GetTasksAsync_GuestOnlySeesOwnTasks()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var guest1 = await CreateGuestAsync(db, "guest-hash-1");
        var guest2 = await CreateGuestAsync(db, "guest-hash-2");
        db.TaskItems.AddRange(Task("Guest one", guestSessionId: guest1.Id), Task("Guest two", guestSessionId: guest2.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(new TaskItemDtos(), null, guest1.Id);

        var task = Assert.Single(result);
        Assert.Equal("Guest one", task.Title);
    }

    [Fact]
    public async Task GetTasksAsync_WithNoOwnerReturnsEmpty()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.Add(Task("Task", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(new TaskItemDtos(), null, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTaskByIdAsync_RespectsOwner()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user1 = await CreateUserAsync(db, "user1");
        var user2 = await CreateUserAsync(db, "user2");
        var task = Task("Private", userId: user1.Id);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var repository = new TaskRepository(db);
        var visible = await repository.GetTaskByIdAsync(task.Id, user1.Id, null);
        var hidden = await repository.GetTaskByIdAsync(task.Id, user2.Id, null);

        Assert.NotNull(visible);
        Assert.Null(hidden);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithNoOwnerReturnsNull()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var task = Task("Task", userId: user.Id);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTaskByIdAsync(task.Id, null, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTasksAsync_FiltersByStatus()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Pending", userId: user.Id, status: TaskStatus.Pending),
            Task("Done", userId: user.Id, status: TaskStatus.Completed));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Status = TaskStatus.Completed }, user.Id, null);

        var task = Assert.Single(result);
        Assert.Equal("Done", task.Title);
    }

    [Fact]
    public async Task GetTasksAsync_FiltersCategoryCaseInsensitively()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Work", userId: user.Id, category: "Work"),
            Task("Home", userId: user.Id, category: "Home"));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Category = "wOrK" }, user.Id, null);

        var task = Assert.Single(result);
        Assert.Equal("Work", task.Title);
    }

    [Fact]
    public async Task GetTasksAsync_FiltersDueDateRangeInclusively()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var start = new DateTime(2026, 9, 10);
        var end = new DateTime(2026, 9, 20);
        db.TaskItems.AddRange(
            Task("Before", userId: user.Id, dueDate: start.AddDays(-1)),
            Task("Start", userId: user.Id, dueDate: start),
            Task("Middle", userId: user.Id, dueDate: start.AddDays(5)),
            Task("End", userId: user.Id, dueDate: end),
            Task("After", userId: user.Id, dueDate: end.AddDays(1)),
            Task("No due date", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { DueAfter = start, DueBefore = end }, user.Id, null);

        Assert.Equal(new[] { "Start", "Middle", "End" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_SearchesTitleAndDescription()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Buy milk", userId: user.Id, description: "From the supermarket"),
            Task("Write report", userId: user.Id, description: "Contains milk budget"),
            Task("Walk dog", userId: user.Id, description: "Evening walk"));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Search = "milk" }, user.Id, null);

        Assert.Equal(new[] { "Buy milk", "Write report" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_TrimsSearchText()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.Add(Task("Buy milk", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Search = "  milk  " }, user.Id, null);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetTasksAsync_SortsByTitleAscending()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Zebra", userId: user.Id),
            Task("Alpha", userId: user.Id),
            Task("Middle", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { SortBy = "title" }, user.Id, null);

        Assert.Equal(new[] { "Alpha", "Middle", "Zebra" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_SortsByTitleDescending()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Zebra", userId: user.Id),
            Task("Alpha", userId: user.Id),
            Task("Middle", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { SortBy = "title", Desc = true }, user.Id, null);

        Assert.Equal(new[] { "Zebra", "Middle", "Alpha" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_SortsByStatus()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(
            Task("Completed", userId: user.Id, status: TaskStatus.Completed),
            Task("Pending", userId: user.Id, status: TaskStatus.Pending),
            Task("In progress", userId: user.Id, status: TaskStatus.InProgress));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { SortBy = "status" }, user.Id, null);

        Assert.Equal(
            new[] { TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Completed },
            result.Select(t => t.Status));
    }

    [Fact]
    public async Task GetTasksAsync_DefaultsToDueDateAscending()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var baseDate = new DateTime(2026, 9, 1);
        db.TaskItems.AddRange(
            Task("Later", userId: user.Id, dueDate: baseDate.AddDays(2)),
            Task("Earlier", userId: user.Id, dueDate: baseDate),
            Task("No date", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(new TaskItemDtos(), user.Id, null);

        Assert.Equal(new[] { "No date", "Earlier", "Later" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_PaginatesWithOffsetAndLimit()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var baseDate = new DateTime(2026, 9, 1);
        db.TaskItems.AddRange(
            Task("One", userId: user.Id, dueDate: baseDate),
            Task("Two", userId: user.Id, dueDate: baseDate.AddDays(1)),
            Task("Three", userId: user.Id, dueDate: baseDate.AddDays(2)),
            Task("Four", userId: user.Id, dueDate: baseDate.AddDays(3)));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Offset = 1, Limit = 2 }, user.Id, null);

        Assert.Equal(new[] { "Two", "Three" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task GetTasksAsync_ZeroOffsetAndLimitDoNotRestrictResults()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.AddRange(Task("One", userId: user.Id), Task("Two", userId: user.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(
            new TaskItemDtos { Offset = 0, Limit = 0 }, user.Id, null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTasksAsync_NullQueryParamsReturnsAllOwnedTasks()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user1 = await CreateUserAsync(db, "user1");
        var user2 = await CreateUserAsync(db, "user2");
        db.TaskItems.AddRange(
            Task("One", userId: user1.Id),
            Task("Two", userId: user1.Id),
            Task("Other", userId: user2.Id));
        await db.SaveChangesAsync();

        var result = await new TaskRepository(db).GetTasksAsync(null!, user1.Id, null);

        Assert.Equal(new[] { "One", "Two" }, result.Select(t => t.Title));
    }

    [Fact]
    public async Task AddTaskAndSaveChanges_PersistsTask()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var repository = new TaskRepository(db);
        var task = Task("New task", userId: user.Id);

        repository.AddTask(task);
        var saved = await repository.SaveChangesAsync();

        Assert.True(saved);
        Assert.NotEqual(0, task.Id);
        Assert.Equal("New task", (await db.TaskItems.SingleAsync()).Title);
    }

    [Fact]
    public async Task DeleteTaskAndSaveChanges_RemovesTask()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var user = await CreateUserAsync(db, "user1");
        var task = Task("Delete me", userId: user.Id);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var repository = new TaskRepository(db);
        repository.DeleteTask(task);
        var saved = await repository.SaveChangesAsync();

        Assert.True(saved);
        Assert.Empty(await db.TaskItems.ToListAsync());
    }

    [Fact]
    public async Task ClaimGuestTasksAsync_ReassignsOnlySpecifiedGuestTasks()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var guest1 = await CreateGuestAsync(db, "guest-hash-1");
        var guest2 = await CreateGuestAsync(db, "guest-hash-2");
        var user = await CreateUserAsync(db, "existing-user");
        db.TaskItems.AddRange(
            Task("Claim one", guestSessionId: guest1.Id),
            Task("Claim two", guestSessionId: guest1.Id),
            Task("Other guest", guestSessionId: guest2.Id),
            Task("Existing user task", userId: user.Id));
        await db.SaveChangesAsync();

        var repository = new TaskRepository(db);
        var newUser = await CreateUserAsync(db, "new-user");
        var claimed = await repository.ClaimGuestTasksAsync(guest1.Id, newUser.Id);

        Assert.Equal(2, claimed);

        var tasks = await db.TaskItems.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
        Assert.Equal((newUser.Id, null), (tasks[0].UserId, tasks[0].GuestSessionId));
        Assert.Equal((newUser.Id, null), (tasks[1].UserId, tasks[1].GuestSessionId));
        Assert.Equal((null, guest2.Id), (tasks[2].UserId, tasks[2].GuestSessionId));
        Assert.Equal((user.Id, null), (tasks[3].UserId, tasks[3].GuestSessionId));
    }

    [Fact]
    public async Task ClaimGuestTasksAsync_WhenNoTasksMatchReturnsZero()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var guest1 = await CreateGuestAsync(db, "guest-hash-1");
        var guest2 = await CreateGuestAsync(db, "guest-hash-2");
        db.TaskItems.Add(Task("Other guest", guestSessionId: guest2.Id));
        await db.SaveChangesAsync();
        var user = await CreateUserAsync(db, "user1");

        var repository = new TaskRepository(db);
        var claimed = await repository.ClaimGuestTasksAsync(guest1.Id, user.Id);

        Assert.Equal(0, claimed);
        var task = await db.TaskItems.SingleAsync();
        Assert.Equal(guest2.Id, task.GuestSessionId);
        Assert.Null(task.UserId);
    }

    [Fact]
    public async Task ClaimGuestTasksAsync_MakesClaimedTasksVisibleToUser()
    {
        using var testDb = CreateContext();
        var db = testDb.Context;
        var guest = await CreateGuestAsync(db, "guest-hash");
        var user = await CreateUserAsync(db, "user1");
        db.TaskItems.Add(Task("Guest task", guestSessionId: guest.Id));
        await db.SaveChangesAsync();

        var repository = new TaskRepository(db);
        await repository.ClaimGuestTasksAsync(guest.Id, user.Id);

        var result = await repository.GetTasksAsync(new TaskItemDtos(), user.Id, null);

        var task = Assert.Single(result);
        Assert.Equal("Guest task", task.Title);
        Assert.Equal(user.Id, task.UserId);
        Assert.Null(task.GuestSessionId);
    }

    private static TestDatabase CreateContext()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mytasks-tests-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MyTasksContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        var context = new MyTasksContext(options);
        context.Database.EnsureCreated();
        return new TestDatabase(context, path);
    }

    private static async Task<User> CreateUserAsync(MyTasksContext db, string username)
    {
        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = "test-password-hash",
            CreatedAt = new DateTime(2026, 9, 1)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<GuestSession> CreateGuestAsync(MyTasksContext db, string tokenHash)
    {
        var now = DateTime.UtcNow;
        var guest = new GuestSession
        {
            TokenHash = tokenHash,
            CreatedAt = now,
            LastAccessedAt = now,
            ExpiresAt = now.AddHours(1)
        };
        db.GuestSessions.Add(guest);
        await db.SaveChangesAsync();
        return guest;
    }

    private sealed class TestDatabase(MyTasksContext context, string path) : IDisposable
    {
        public MyTasksContext Context { get; } = context;

        public void Dispose()
        {
            Context.Dispose();
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup of the temporary SQLite file.
            }
        }
    }

    private static TaskItem Task(
        string title,
        int? userId = null,
        int? guestSessionId = null,
        MyTasks.Models.TaskStatus status = MyTasks.Models.TaskStatus.Pending,
        DateTime? dueDate = null,
        string category = "General",
        string description = "")
    {
        return new TaskItem
        {
            Title = title,
            Description = description,
            DueDate = dueDate,
            Category = category,
            Status = status,
            CreatedAt = new DateTime(2026, 9, 1),
            UpdatedAt = new DateTime(2026, 9, 1),
            UserId = userId,
            GuestSessionId = guestSessionId
        };
    }
}
