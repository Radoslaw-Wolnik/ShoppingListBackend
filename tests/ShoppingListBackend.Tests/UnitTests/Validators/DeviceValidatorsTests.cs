using FluentAssertions;
using FluentValidation.TestHelper;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.Validators.Device;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Validators;

public class DeviceValidatorsTests
{
    private readonly UpdateUsernameRequestValidator _usernameValidator = new();
    private readonly UpdateColourRequestValidator _colourValidator = new();
    private readonly AddFriendRequestValidator _friendValidator = new();
    private readonly RegisterDeviceRequestValidator _registerValidator = new();

    [Fact]
    public void UpdateUsernameRequestValidator_ShouldPass_WhenValid()
    {
        var request = new UpdateUsernameRequest { UserName = "ValidUser" };
        var result = _usernameValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateUsernameRequestValidator_ShouldFail_WhenUserNameEmpty()
    {
        var request = new UpdateUsernameRequest { UserName = "" };
        var result = _usernameValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("Username is required.");
    }

    [Fact]
    public void UpdateUsernameRequestValidator_ShouldFail_WhenUserNameTooLong()
    {
        var request = new UpdateUsernameRequest { UserName = new string('a', 51) };
        var result = _usernameValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserName)
            .WithErrorMessage("Username cannot exceed 50 characters.");
    }

    [Fact]
    public void UpdateColourRequestValidator_ShouldPass_WhenValidHex()
    {
        var request = new UpdateColourRequest { Colour = "#FF00AA" };
        var result = _colourValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateColourRequestValidator_ShouldFail_WhenColourEmpty()
    {
        var request = new UpdateColourRequest { Colour = "" };
        var result = _colourValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Colour)
            .WithErrorMessage("Colour is required.");
    }

    [Fact]
    public void UpdateColourRequestValidator_ShouldFail_WhenInvalidHex()
    {
        var request = new UpdateColourRequest { Colour = "FF00AA" };
        var result = _colourValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Colour)
            .WithErrorMessage("Colour must be a valid hex code (e.g., #FF0000).");
    }

    [Fact]
    public void AddFriendRequestValidator_ShouldPass_WhenValid()
    {
        var request = new AddFriendRequest { FriendDeviceId = Guid.NewGuid() };
        var result = _friendValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddFriendRequestValidator_ShouldFail_WhenFriendDeviceIdEmpty()
    {
        var request = new AddFriendRequest { FriendDeviceId = Guid.Empty };
        var result = _friendValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FriendDeviceId)
            .WithErrorMessage("Friend device ID must be a valid GUID.");
    }

    [Fact]
    public void RegisterDeviceRequestValidator_ShouldPass_Always()
    {
        var request = new RegisterDeviceRequest();
        var result = _registerValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}