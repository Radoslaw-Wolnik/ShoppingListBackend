using FluentValidation;
using ShoppingListBackend.Api.DTOs.Device;

namespace ShoppingListBackend.Api.Validators.Device;

public class UpdateUsernameRequestValidator : AbstractValidator<UpdateUsernameRequest>
{
    public UpdateUsernameRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");
    }
}
