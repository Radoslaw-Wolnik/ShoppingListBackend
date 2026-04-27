using FluentAssertions;
using ShoppingListBackend.Api.Services;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Services;

public class BcryptHashServiceTests
{
    private readonly BcryptHashService _hashService = new();

    [Fact]
    public void Hash_Returns_NonEmptyString()
    {
        var input = "myPassword123";
        var hash = _hashService.Hash(input);
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(input);
    }

    [Fact]
    public void SameInput_Produces_DifferentHashes()
    {
        var input = "samePassword";
        var hash1 = _hashService.Hash(input);
        var hash2 = _hashService.Hash(input);
        hash1.Should().NotBe(hash2); // BCrypt uses random salt
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectInput()
    {
        var input = "correctPassword";
        var hash = _hashService.Hash(input);
        var result = _hashService.Verify(input, hash);
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectInput()
    {
        var input = "correctPassword";
        var wrongInput = "wrongPassword";
        var hash = _hashService.Hash(input);
        var result = _hashService.Verify(wrongInput, hash);
        result.Should().BeFalse();
    }
}