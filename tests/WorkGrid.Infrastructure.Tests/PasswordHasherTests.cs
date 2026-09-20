using WorkGrid.Domain.Exceptions;
using WorkGrid.Infrastructure.Services;
using Xunit;

namespace WorkGrid.Infrastructure.Tests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesNonPlaintextSaltedHash()
    {
        const string password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.DoesNotContain(password, hash);
        Assert.Contains(".", hash); // Format: iterations.salt.hash
    }

    [Fact]
    public void HashPassword_GeneratesUniqueSaltsForSamePassword()
    {
        const string password = "SamePassword123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "CorrectPassword!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        const string password = "CorrectPassword!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword("WrongPassword!", hash);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_WithEmptyPassword_ThrowsDomainValidationException(string? password)
    {
        Assert.Throws<DomainValidationException>(() => _hasher.HashPassword(password!));
    }
}
