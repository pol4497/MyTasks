using System.Net.Http.Headers;
using System.Net.Http.Json;
using MyTasks.Dtos;

namespace MyTasks.IntegrationTests.Infrastructure;

public static class TestAuthHelper
{
    public static async Task<GuestSessionResponseDto> CreateGuestSessionAsync(
        HttpClient client)
    {
        var response = await client.PostAsync(
            "/api/guest/session",
            content: null);

        response.EnsureSuccessStatusCode();

        var session = await response.Content
            .ReadFromJsonAsync<GuestSessionResponseDto>();

        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session.GuestToken));

        return session;
    }

    public static async Task<AuthResponseDto> RegisterAndLoginAsync(
        HttpClient client,
        string username,
        string email,
        string password = "TestPassword123!")
    {
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = email,
                Password = password
            });

        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto
            {
                Username = username,
                Password = password
            });

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content
            .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        return auth;
    }
}