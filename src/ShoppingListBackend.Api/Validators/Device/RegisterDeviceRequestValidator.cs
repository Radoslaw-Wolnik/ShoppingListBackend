using FluentValidation;
using ShoppingListBackend.Api.DTOs.Device;

namespace ShoppingListBackend.Api.Validators.Device;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        // No fields to validate; but we keep the validator for consistency.
    }
}