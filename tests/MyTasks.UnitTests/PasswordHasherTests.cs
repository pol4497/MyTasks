using System.Security.Cryptography;
using System.Text;
using MyTasks.Security;

namespace MyTasks.UnitTests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ProducesNonEmptyHash()
    {
        var hash = PasswordHasher.Hash("TestPassword123!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Hash_ProducesArgon2idFormat()
    {
        var hash = PasswordHasher.Hash("TestPassword123!");
        var parts = hash.Split('.');

        Assert.Equal(5, parts.Length);
        Assert.Equal("2", parts[0]);
        Assert.Equal("19456", parts[1]);
        Assert.Equal("1", parts[2]);
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        var firstHash = PasswordHasher.Hash("TestPassword123!");
        var secondHash = PasswordHasher.Hash("TestPassword123!");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        const string password = "TestPassword123!";
        var hash = PasswordHasher.Hash(password);

        Assert.True(PasswordHasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForIncorrectPassword()
    {
        var hash = PasswordHasher.Hash("TestPassword123!");

        Assert.False(PasswordHasher.Verify("WrongPassword123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForMalformedHash()
    {
        Assert.False(PasswordHasher.Verify("TestPassword123!", "not-a-valid-hash"));
    }

    [Fact]
    public void Verify_SupportsLegacyPbkdf2Hash()
    {
        const string password = "LegacyPassword123!";
        const int iterations = 100_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        var legacyHash = string.Join(
            '.',
            iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(derivedKey));

        Assert.True(PasswordHasher.Verify(password, legacyHash));
        Assert.False(PasswordHasher.Verify("WrongPassword123!", legacyHash));
    }
}
