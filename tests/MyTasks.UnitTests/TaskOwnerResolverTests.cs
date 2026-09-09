using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyTasks.Contexts;
using MyTasks.Models;
using MyTasks.Repositories;
using MyTasks.Services;

namespace MyTasks.UnitTests;

public class TaskOwnerResolverTests
{
    private static TaskOwnerResolver CreateSut(
        IHttpContextAccessor accessor,
        IGuestSessionRepository repository,
        IGuestTokenService tokens,
        ITaskOwnerContext ownerContext) =>
        new(accessor, repository, tokens, ownerContext);

    private sealed class FakeGuestSessionRepository : IGuestSessionRepository
    {
        public GuestSession? Session { get; init; }
        public string? LastLookupHash { get; private set; }
        public bool TouchCalled { get; private set; }
        public int TouchedSessionId { get; private set; }

        public Task<GuestSession?> GetByTokenHashAsync(string tokenHash)
        {
            LastLookupHash = tokenHash;
            return Task.FromResult(Session);
        }

        public void Add(GuestSession session) => throw new NotSupportedException();

        public Task TouchAsync(int guestSessionId, DateTime now)
        {
            TouchCalled = true;
            TouchedSessionId = guestSessionId;
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(int guestSessionId, DateTime now) =>
            throw new NotSupportedException();

        public Task<bool> SaveChangesAsync() => throw new NotSupportedException();
    }

    private sealed class FakeGuestTokenService : IGuestTokenService
    {
        public string HashResult { get; init; } = "hash";
        public string? LastRawToken { get; private set; }

        public string Generate() => throw new NotSupportedException();

        public string Hash(string rawToken)
        {
            LastRawToken = rawToken;
            return HashResult;
        }
    }

    [Fact]
    public async Task ResolveAsync_NoHttpContext_ReturnsFalse()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var repository = new FakeGuestSessionRepository();
        var tokens = new FakeGuestTokenService();
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(accessor, repository, tokens, ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
        Assert.Null(ownerContext.UserId);
        Assert.Null(ownerContext.GuestSessionId);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithValidClaim_SetsUserOwner()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")],
                authenticationType: "Test"))
        };

        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            new FakeGuestSessionRepository(),
            new FakeGuestTokenService(),
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.True(result);
        Assert.Equal(42, ownerContext.UserId);
        Assert.Null(ownerContext.GuestSessionId);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithInvalidClaim_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "not-an-id")],
                authenticationType: "Test"))
        };

        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            new FakeGuestSessionRepository(),
            new FakeGuestTokenService(),
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
        Assert.Null(ownerContext.UserId);
        Assert.Null(ownerContext.GuestSessionId);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUserWithoutClaim_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                authenticationType: "Test"))
        };

        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            new FakeGuestSessionRepository(),
            new FakeGuestTokenService(),
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ResolveAsync_NoAuthenticationAndNoGuestToken_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            new FakeGuestSessionRepository(),
            new FakeGuestTokenService(),
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
        Assert.Null(ownerContext.UserId);
        Assert.Null(ownerContext.GuestSessionId);
    }

    [Fact]
    public async Task ResolveAsync_WhitespaceGuestToken_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Guest-Token"] = "   ";
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            new FakeGuestSessionRepository(),
            new FakeGuestTokenService(),
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ResolveAsync_InvalidGuestToken_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Guest-Token"] = "invalid-token";
        var repository = new FakeGuestSessionRepository();
        var tokens = new FakeGuestTokenService { HashResult = "hash-for-invalid-token" };
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            repository,
            tokens,
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
        Assert.Equal("invalid-token", tokens.LastRawToken);
        Assert.Equal("hash-for-invalid-token", repository.LastLookupHash);
        Assert.False(repository.TouchCalled);
    }

    [Fact]
    public async Task ResolveAsync_ExpiredGuestSession_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Guest-Token"] = "guest-token";
        var repository = new FakeGuestSessionRepository
        {
            Session = new GuestSession
            {
                Id = 7,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            }
        };
        var tokens = new FakeGuestTokenService { HashResult = "hash" };
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            repository,
            tokens,
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.False(result);
        Assert.Null(ownerContext.GuestSessionId);
        Assert.False(repository.TouchCalled);
    }

    [Fact]
    public async Task ResolveAsync_ActiveGuestSession_SetsGuestOwnerAndTouchesSession()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Guest-Token"] = "guest-token";
        var repository = new FakeGuestSessionRepository
        {
            Session = new GuestSession
            {
                Id = 7,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            }
        };
        var tokens = new FakeGuestTokenService { HashResult = "hash" };
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            repository,
            tokens,
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.True(result);
        Assert.Equal(7, ownerContext.GuestSessionId);
        Assert.Null(ownerContext.UserId);
        Assert.True(repository.TouchCalled);
        Assert.Equal(7, repository.TouchedSessionId);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticatedUser_TakesPrecedenceOverGuestToken()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")],
                authenticationType: "Test"))
        };
        httpContext.Request.Headers["X-Guest-Token"] = "guest-token";

        var repository = new FakeGuestSessionRepository
        {
            Session = new GuestSession
            {
                Id = 7,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            }
        };
        var ownerContext = new TaskOwnerContext();
        var sut = CreateSut(
            new HttpContextAccessor { HttpContext = httpContext },
            repository,
            new FakeGuestTokenService { HashResult = "hash" },
            ownerContext);

        var result = await sut.ResolveAsync();

        Assert.True(result);
        Assert.Equal(42, ownerContext.UserId);
        Assert.Null(ownerContext.GuestSessionId);
        Assert.False(repository.TouchCalled);
    }
}
