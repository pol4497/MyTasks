using System.Net;
using System.Net.Http.Json;
using MyTasks.IntegrationTests.Infrastructure;
using MyTasks.Dtos;

namespace MyTasks.IntegrationTests;

public class GuestSessionTests : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GuestSessionTests(MyTasksWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGuestSession_ReturnsCreated_WithToken()
    {
        var response = await _client.PostAsync(
            "/api/guest/session",
            content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<GuestSessionResponseDto>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.GuestToken));
    }
}