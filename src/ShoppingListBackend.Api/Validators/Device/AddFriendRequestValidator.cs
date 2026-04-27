using FluentValidation;
using ShoppingListBackend.Api.DTOs.Device;

namespace ShoppingListBackend.Api.Validators.Device;

public class AddFriendRequestValidator : AbstractValidator<AddFriendRequest>
{
    public AddFriendRequestValidator()
    {
        RuleFor(x => x.FriendDeviceId)
            .NotEmpty().WithMessage("Friend device ID is required.")
            .NotEqual(Guid.Empty).WithMessage("Friend device ID must be a valid GUID.");
    }
}
