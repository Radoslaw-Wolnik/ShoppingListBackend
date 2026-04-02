using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class AddItemRequestValidator : AbstractValidator<AddItemRequest>
{
    public AddItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Item description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}