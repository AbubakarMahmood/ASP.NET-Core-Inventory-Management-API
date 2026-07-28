using FluentAssertions;
using InventoryAPI.Infrastructure.Services;

namespace InventoryAPI.UnitTests.Services;

public class RefreshTokenHasherTests
{
    private readonly RefreshTokenHasher _hasher = new();

    [Fact]
    public void Hash_IsDeterministicHexDigestAndNotRawSecret()
    {
        const string token = "high-entropy-refresh-token";
        var first = _hasher.Hash(token);
        _hasher.Hash(token).Should().Be(first);
        first.Should().NotBe(token);
        first.Should().HaveLength(64);
    }

    [Fact]
    public void Verify_MatchingToken_ReturnsTrue()
    {
        var hash = _hasher.Hash("token");
        _hasher.Verify("token", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongOrMalformedHash_ReturnsFalse()
    {
        var hash = _hasher.Hash("token");
        _hasher.Verify("other", hash).Should().BeFalse();
        _hasher.Verify("token", "not-hex").Should().BeFalse();
    }
}
