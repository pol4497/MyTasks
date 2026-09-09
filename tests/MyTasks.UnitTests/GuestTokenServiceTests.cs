using MyTasks.Services;

namespace MyTasks.UnitTests;

public class GuestTokenServiceTests
{
    private readonly GuestTokenService _service = new();

    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var token = _service.Generate();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_ReturnsDifferentTokens()
    {
        var first = _service.Generate();
        var second = _service.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Generate_ReturnsUrlSafeBase64WithoutPadding()
    {
        var token = _service.Generate();

        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
        Assert.DoesNotContain("=", token);
    }

    [Fact]
    public void Hash_IsDeterministicForSameToken()
    {
        const string rawToken = "guest-token";

        var firstHash = _service.Hash(rawToken);
        var secondHash = _service.Hash(rawToken);

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForDifferentTokens()
    {
        var firstHash = _service.Hash("guest-token-a");
        var secondHash = _service.Hash("guest-token-b");

        Assert.NotEqual(firstHash, secondHash);
    }
}
