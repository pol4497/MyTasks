using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;

namespace MyTasks.IntegrationTests;

public class ErrorHandlingTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DuplicateUsername_ReturnsProblemDetails()
    {
        var username = $"problem_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        await TestAuthHelper.RegisterAndLoginAsync(_client, username, email);
        TestAuthHelper.ClearAuthentication(_client);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = $"other_{username}@example.com",
                Password = "TestPassword123!"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Conflict", root.GetProperty("title").GetString());
        Assert.Equal("ConflictException", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("requestId", out var requestId));
        Assert.False(string.IsNullOrWhiteSpace(requestId.GetString()));
    }

    [Fact]
    public async Task InvalidRefreshToken_ReturnsProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequestDto { RefreshToken = "invalid-refresh-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(401, root.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", root.GetProperty("title").GetString());
        Assert.Equal("UnauthorizedException", root.GetProperty("type").GetString());
    }
}
