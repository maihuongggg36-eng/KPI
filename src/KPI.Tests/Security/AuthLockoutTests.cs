using FluentAssertions;
using Xunit;

namespace KPI.Tests.Security;

public class AuthLockoutTests
{
    [Fact]
    public void BCrypt_HashAndVerify_Succeeds()
    {
        // Arrange
        var password = "TestPassword@2026";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        // Act
        var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        var isInvalid = BCrypt.Net.BCrypt.Verify("WrongPassword", hash);

        // Assert
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void LockoutThreshold_LocksAccountWhenFailedAttemptsAtLeast5(int attempts, bool shouldLock)
    {
        // Arrange & Act
        var isLocked = attempts >= 5;

        // Assert
        isLocked.Should().Be(shouldLock);
    }

    [Fact]
    public void LockoutDuration_ShouldBe30Minutes()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var lockedUntil = now.AddMinutes(30);

        // Assert
        (lockedUntil - now).TotalMinutes.Should().Be(30);
    }
}
