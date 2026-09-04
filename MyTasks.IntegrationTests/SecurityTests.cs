using System.Net;
using System.Net.Http.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;

namespace MyTasks.IntegrationTests;

public class SecurityTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task InvalidGuestToken_ReturnsUnauthorized()
    {
        TestAuthHelper.UseGuestToken(_client, "not-a-real-guest-token");

        var response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotUpdateAnotherUsersTask()
    {
        var userA = $"updatea_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userA, $"{userA}@example.com");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Private task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(task);

        TestAuthHelper.ClearAuthentication(_client);
        var userB = $"updateb_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userB, $"{userB}@example.com");

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}",
            new TaskUpdateDto { Title = "Hacked" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task User_CannotDeleteAnotherUsersTask()
    {
        var userA = $"deletea_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userA, $"{userA}@example.com");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "Private task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(task);

        TestAuthHelper.ClearAuthentication(_client);
        var userB = $"deleteb_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, userB, $"{userB}@example.com");

        var response = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Guest_CannotReadUsersTask()
    {
        var username = $"guestvsuser_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new TaskCreateDto { Title = "User task" });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskReadDto>();
        Assert.NotNull(task);

        TestAuthHelper.ClearAuthentication(_client);
        var guest = await TestAuthHelper.CreateGuestSessionAsync(_client);
        TestAuthHelper.UseGuestToken(_client, guest.GuestToken);

        var response = await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NonexistentTask_ReturnsNotFound()
    {
        var username = $"notfound_{Guid.NewGuid():N}";
        await TestAuthHelper.RegisterAndLoginAsync(_client, username, $"{username}@example.com");

        var response = await _client.GetAsync("/api/tasks/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
