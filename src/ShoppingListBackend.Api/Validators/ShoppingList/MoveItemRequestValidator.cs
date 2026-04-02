using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class MoveItemRequestValidator : AbstractValidator<MoveItemRequest>
{
    public MoveItemRequestValidator()
    {
        RuleFor(x => x.NewCategoryId)
            .NotEmpty().WithMessage("New category ID is required.")
            .NotEqual(Guid.Empty).WithMessage("New category ID must be a valid GUID.");
    }
}