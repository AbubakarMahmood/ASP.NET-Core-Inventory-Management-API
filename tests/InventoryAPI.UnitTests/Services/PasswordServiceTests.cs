using FluentAssertions;
using InventoryAPI.Infrastructure.Services;

namespace InventoryAPI.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void HashPassword_ThenVerify_Succeeds()
    {
        var hash = _service.HashPassword("Str0ngPassword!");

        _service.VerifyPassword("Str0ngPassword!", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_Fails()
    {
        var hash = _service.HashPassword("Str0ngPassword!");

        _service.VerifyPassword("WrongPassword1", hash).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes()
    {
        var first = _service.HashPassword("Str0ngPassword!");
        var second = _service.HashPassword("Str0ngPassword!");

        first.Should().NotBe(second, "each hash must use a unique salt");
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("dG9vc2hvcnQ=")] // valid Base64 but shorter than salt + hash
    [InlineData("")]
    public void VerifyPassword_MalformedStoredHash_ReturnsFalse(string storedHash)
    {
        _service.VerifyPassword("anything", storedHash).Should().BeFalse();
    }
}
