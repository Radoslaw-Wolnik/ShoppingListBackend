using FluentValidation;
using ShoppingListBackend.Api.DTOs.Device;

namespace ShoppingListBackend.Api.Validators.Device;

public class UpdateColourRequestValidator : AbstractValidator<UpdateColourRequest>
{
    public UpdateColourRequestValidator()
    {
        RuleFor(x => x.Colour)
            .NotEmpty().WithMessage("Colour is required.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Colour must be a valid hex code (e.g., #FF0000).");
    }
}