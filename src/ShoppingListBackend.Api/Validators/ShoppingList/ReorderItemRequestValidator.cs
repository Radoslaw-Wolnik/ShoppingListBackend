using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class ReorderItemRequestValidator : AbstractValidator<ReorderItemRequest>
{
    public ReorderItemRequestValidator()
    {
        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(0).WithMessage("Position must be non-negative.");
    }
}
