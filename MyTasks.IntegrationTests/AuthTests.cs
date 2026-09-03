using System.Net;
using System.Net.Http.Json;
using MyTasks.Dtos;
using MyTasks.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MyTasks.Data;
using MyTasks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MyTasks.IntegrationTests;

public class AuthTests(MyTasksWebApplicationFactory factory) : IClassFixture<MyTasksWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_CreatesUser()
    {
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = email,
                Password = "TestPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var user =
            await response.Content.ReadFromJsonAsync<UserReadDto>();

        Assert.NotNull(user);
        Assert.True(user.Id > 0);
        Assert.Equal(username, user.Username);
        Assert.Equal(email, user.Email);
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public async Task Register_WithTooShortUsername_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = "ab",
                Email = "valid@example.com",
                Password = "TestPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = $"valid_{Guid.NewGuid():N}",
                Email = "not-an-email",
                Password = "TestPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var username = $"duplicate_{Guid.NewGuid():N}";

        var first = new RegisterDto
        {
            Username = username,
            Email = $"{username}1@example.com",
            Password = "TestPassword123!"
        };

        var second = new RegisterDto
        {
            Username = username,
            Email = $"{username}2@example.com",
            Password = "TestPassword123!"
        };

        var firstResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                first);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                second);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"{suffix}@example.com";

        var firstResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterDto
                {
                    Username = $"user1_{suffix}",
                    Email = email,
                    Password = "TestPassword123!"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterDto
                {
                    Username = $"user2_{suffix}",
                    Email = email,
                    Password = "TestPassword123!"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var username = $"login_{Guid.NewGuid():N}";
        var password = "TestPassword123!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto
            {
                Username = username,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var auth =
            await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(username, auth.User.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var username = $"wrongpass_{Guid.NewGuid():N}";

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = "CorrectPassword123!"
            });

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto
            {
                Username = username,
                Password = "WrongPassword123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var username = $"logout_{Guid.NewGuid():N}";
        var password = "TestPassword123!";

        var auth =
            await TestAuthHelper.RegisterAndLoginAsync(
                _client,
                username,
                $"{username}@example.com",
                password);

        var logoutResponse = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new RefreshRequestDto
            {
                RefreshToken = auth.RefreshToken
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        // The refresh token should no longer work.
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequestDto
            {
                RefreshToken = auth.RefreshToken
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidAccessToken_ReturnsCurrentUser()
    {
        var username = $"me_{Guid.NewGuid():N}";

        var auth =
            await TestAuthHelper.RegisterAndLoginAsync(
                _client,
                username,
                $"{username}@example.com");

        var response =
            await _client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var user =
            await response.Content.ReadFromJsonAsync<UserReadDto>();

        Assert.NotNull(user);
        Assert.Equal(auth.User.Id, user.Id);
        Assert.Equal(username, user.Username);
    }

    [Fact]
    public async Task Refresh_ReturnsNewTokens_AndInvalidatesOldRefreshToken()
    {
        var username = $"refresh_{Guid.NewGuid():N}";
        var password = "TestPassword123!";

        await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = $"{username}@example.com",
                Password = password
            });

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto
            {
                Username = username,
                Password = password
            });

        var original =
            await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(original);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequestDto
            {
                RefreshToken = original.RefreshToken
            });

        Assert.Equal(
            HttpStatusCode.OK,
            refreshResponse.StatusCode);

        var refreshed =
            await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(refreshed);
        Assert.NotEqual(
            original.RefreshToken,
            refreshed.RefreshToken);
        Assert.NotEqual(
            original.AccessToken,
            refreshed.AccessToken);

        // The original refresh token must now be unusable.
        var secondAttempt = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequestDto
            {
                RefreshToken = original.RefreshToken
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            secondAttempt.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsUsers()
    {
        var username = $"admin_{Guid.NewGuid():N}";
        var email = $"{username}@example.com";
        var password = "AdminPassword123!";

        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto
            {
                Username = username,
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        // Promote the user directly in the isolated test database.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<MyTasksContext>();

            var user = await db.Users
                .SingleAsync(u => u.Username == username);

            user.Role = UserRole.Admin;

            await db.SaveChangesAsync();
        }

        // Login again so the newly-issued JWT contains Role=Admin.
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new LoginDto
            {
                Username = username,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var auth =
            await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                auth.AccessToken);

        var response =
            await _client.GetAsync("/api/auth/users");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var users =
            await response.Content
                .ReadFromJsonAsync<List<UserReadDto>>();

        Assert.NotNull(users);
        Assert.Contains(users, u => u.Username == username);
    }

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync("/api/auth/users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsNormalUser_ReturnsForbidden()
    {
        var username = $"normal_{Guid.NewGuid():N}";

        await TestAuthHelper.RegisterAndLoginAsync(
            _client,
            username,
            $"{username}@example.com");

        var response =
            await _client.GetAsync("/api/auth/users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}