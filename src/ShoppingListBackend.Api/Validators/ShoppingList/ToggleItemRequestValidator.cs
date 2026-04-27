using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class ToggleItemRequestValidator : AbstractValidator<ToggleItemRequest>
{
    public ToggleItemRequestValidator()
    {
        // No validation needed for a boolean, but we keep the validator.
    }
}
