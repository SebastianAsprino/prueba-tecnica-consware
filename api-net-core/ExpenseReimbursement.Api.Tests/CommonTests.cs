using ExpenseReimbursement.Api.Common;
using Microsoft.Extensions.Time.Testing;

namespace ExpenseReimbursement.Api.Tests;

public class AccessCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsTenDigits()
    {
        var code = AccessCodeGenerator.Generate();

        Assert.Equal(AccessCodeGenerator.Length, code.Length);
        Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
    }

    [Fact]
    public async Task GenerateUniqueAsync_WhenEveryCodeExists_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AccessCodeGenerator.GenerateUniqueAsync(_ => Task.FromResult(true)));
    }
}

public class SecureCompareTests
{
    [Theory]
    [InlineData("secreto", "secreto", true)]
    [InlineData("secreto", "Secreto", false)]
    [InlineData("secreto", "secreto-largo", false)]
    [InlineData(null, "secreto", false)]
    [InlineData("secreto", null, false)]
    public void FixedTimeEquals_ComparesValues(string? left, string? right, bool expected)
    {
        Assert.Equal(expected, SecureCompare.FixedTimeEquals(left, right));
    }
}

public class AdminTokenStoreTests
{
    [Fact]
    public void IssuedToken_IsValidUntilItExpires()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var store = new AdminTokenStore(clock);

        var token = store.Issue();
        Assert.True(store.IsValid(token));

        clock.Advance(AdminTokenStore.TokenLifetime + TimeSpan.FromSeconds(1));
        Assert.False(store.IsValid(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("token-inexistente")]
    public void UnknownToken_IsInvalid(string? token)
    {
        Assert.False(new AdminTokenStore().IsValid(token));
    }
}
