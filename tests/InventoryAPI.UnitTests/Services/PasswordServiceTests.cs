using System.Security.Cryptography;
using FluentAssertions;
using InventoryAPI.Infrastructure.Services;

namespace InventoryAPI.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void HashThenVerify_SucceedsWithoutRehashRequest()
    {
        var hash = _service.HashPassword("Str0ngPassword!");
        hash.Should().StartWith("pbkdf2-sha256$");
        _service.VerifyPassword("Str0ngPassword!", hash).Should().BeTrue();
        _service.NeedsRehash(hash).Should().BeFalse();
    }

    [Fact]
    public void HashSamePasswordTwice_UsesDifferentSalts()
    {
        _service.HashPassword("Str0ngPassword!")
            .Should().NotBe(_service.HashPassword("Str0ngPassword!"));
    }

    [Fact]
    public void WrongPassword_Fails()
    {
        var hash = _service.HashPassword("Str0ngPassword!");
        _service.VerifyPassword("WrongPassword1", hash).Should().BeFalse();
    }

    [Fact]
    public void LegacyHash_VerifiesAndRequestsUpgrade()
    {
        const string password = "Str0ngPassword!";
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var stored = Convert.ToBase64String(salt.Concat(hash).ToArray());

        _service.VerifyPassword(password, stored).Should().BeTrue();
        _service.NeedsRehash(stored).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("dG9vc2hvcnQ=")]
    [InlineData("")]
    [InlineData("pbkdf2-sha256$999999999$c2FsdA==$aGFzaA==")]
    public void MalformedStoredHash_ReturnsFalse(string storedHash)
    {
        _service.VerifyPassword("anything", storedHash).Should().BeFalse();
    }
}
