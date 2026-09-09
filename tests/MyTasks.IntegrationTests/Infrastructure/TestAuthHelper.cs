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

        // Store the access token on the client so subsequent requests are authenticated as this user.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        // Remove any previous guest token because this client is now acting as a registered user.
        client.DefaultRequestHeaders.Remove("X-Guest-Token");

        return auth;
    }

    /// <summary>
    /// Remove both authentication mechanisms so the client represents an unauthenticated caller.
    /// </summary>
    public static void ClearAuthentication(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("X-Guest-Token");
    }

    /// <summary>
    /// Switch the client from user authentication to guest authentication.
    /// </summary>
    public static void UseGuestToken(HttpClient client, string guestToken)
    {
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("X-Guest-Token");
        client.DefaultRequestHeaders.Add("X-Guest-Token", guestToken);
    }
}