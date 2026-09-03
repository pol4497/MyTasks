using System.Net;
using System.Net.Http.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;

namespace MyTasks.IntegrationTests;

public class TaskTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // =========================
    // Task CRUD tests
    // =========================

    [Fact]
    public async Task CreateTask_WithPastDueDate_ReturnsBadRequest()
    {
        var username = $"validation_{Guid.NewGuid():N}";

        await TestAuthHelper.RegisterAndLoginAsync(
            _client,
            username,
            $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Invalid task",
                Description = "Past due",
                Category = "Testing",
                DueDate = DateTime.UtcNow.AddDays(-1)
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // =========================
    // Guest task tests
    // =========================

    [Fact]
    public async Task Guest_CanCreateTask()
    {
        var session =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            session.GuestToken);

        var request = new TaskCreateDto
        {
            Title = "Guest task",
            Description = "Created by integration test",
            Category = "Testing"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task =
            await response.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal("Guest task", task.Title);
        Assert.Equal("Pending", task.Status);
    }

    [Fact]
    public async Task Guest_CanReadOwnTask()
    {
        var session =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            session.GuestToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Read me",
                Description = "Guest task",
                Category = "Testing"
            });

        var created =
            await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(created);

        var response = await _client.GetAsync(
            $"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task =
            await response.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(task);
        Assert.Equal(created.Id, task.Id);
        Assert.Equal("Read me", task.Title);
    }

    [Fact]
    public async Task Guest_CanUpdateOwnTask()
    {
        var session =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            session.GuestToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Original",
                Description = "Original description",
                Category = "Testing"
            });

        var created =
            await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(created);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new TaskUpdateDto
            {
                Title = "Updated",
                Description = "Updated description",
                Category = "Updated",
                Status = Models.TaskStatus.Completed
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        var getResponse =
            await _client.GetAsync($"/api/tasks/{created.Id}");

        var updated =
            await getResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("Updated", updated.Category);
        Assert.Equal("Completed", updated.Status);
    }

    [Fact]
    public async Task Guest_CanDeleteOwnTask()
    {
        var session =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            session.GuestToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Delete me",
                Description = "Temporary task",
                Category = "Testing"
            });

        var created =
            await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(created);

        var deleteResponse =
            await _client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await _client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Tasks_WithoutOwner_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Guest_CannotAccessAnotherGuestsTask()
    {
        // Guest A
        var guestA =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            guestA.GuestToken);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Guest A private task",
                Description = "Should not be visible to Guest B",
                Category = "Security"
            });

        var task =
            await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(task);

        _client.DefaultRequestHeaders.Remove("X-Guest-Token");

        // Guest B
        var guestB =
            await TestAuthHelper.CreateGuestSessionAsync(_client);

        _client.DefaultRequestHeaders.Add(
            "X-Guest-Token",
            guestB.GuestToken);

        var response =
            await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // =========================
    // User task tests
    // =========================

    [Fact]
    public async Task User_CanCreateTask()
    {
        var username = $"taskuser_{Guid.NewGuid():N}";

        await TestAuthHelper.RegisterAndLoginAsync(
            _client,
            username,
            $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "Authenticated task",
                Description = "Owned by a registered user",
                Category = "Testing"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var task =
            await response.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal("Authenticated task", task.Title);
        Assert.Equal("Pending", task.Status);
    }

    [Fact]
    public async Task User_CannotAccessAnotherUsersTask()
    {
        // User A
        var userA = $"usera_{Guid.NewGuid():N}";

        await TestAuthHelper.RegisterAndLoginAsync(
            _client,
            userA,
            $"{userA}@example.com");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto
            {
                Title = "User A private task",
                Description = "Private",
                Category = "Security"
            });

        var task =
            await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();

        Assert.NotNull(task);

        // Remove User A's JWT.
        _client.DefaultRequestHeaders.Authorization = null;

        // User B
        var userB = $"userb_{Guid.NewGuid():N}";

        await TestAuthHelper.RegisterAndLoginAsync(
            _client,
            userB,
            $"{userB}@example.com");

        // User B attempts to access User A's task.
        var response =
            await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}