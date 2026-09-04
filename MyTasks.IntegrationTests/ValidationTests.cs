using System.Net;
using System.Net.Http.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;

namespace MyTasks.IntegrationTests;

public class ValidationTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_MissingRequiredFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto { Username = "validuser", Email = "not-an-email", Password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto { Username = "validuser", Email = "valid@example.com", Password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_EmptyTitle_ReturnsBadRequest()
    {
        var username = $"emptytitle_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_TitleOverMaxLength_ReturnsBadRequest()
    {
        var username = $"longtitle_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = new string('x', 201) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_DescriptionOverMaxLength_ReturnsBadRequest()
    {
        var username = $"longdescription_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Valid", Description = new string('x', 1001) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_CategoryOverMaxLength_ReturnsBadRequest()
    {
        var username = $"longcategory_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Valid", Category = new string('x', 101) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_PastDueDate_ReturnsBadRequest()
    {
        var username = $"pastdue_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Past due", DueDate = DateTime.UtcNow.AddDays(-1) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_FutureDueDate_ReturnsCreated()
    {
        var username = $"futuredue_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Future due", DueDate = DateTime.UtcNow.AddDays(1) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_PastDueDate_ReturnsBadRequest()
    {
        var username = $"updatepastdue_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "Task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(task);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new TaskUpdateDto { Title = "Task", DueDate = DateTime.UtcNow.AddDays(-1) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_UndefinedStatusValue_ReturnsBadRequest()
    {
        var username = $"invalidstatus_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", new TaskCreateDto { Title = "Task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(task);

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new
            {
                title = "Task",
                description = "",
                dueDate = (DateTime?)null,
                category = "",
                status = 99
            });

        // This is an intentional contract test. It currently exposes the missing
        // Enum.IsDefined validation in TaskUpdateDto if the API returns 204 instead.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
