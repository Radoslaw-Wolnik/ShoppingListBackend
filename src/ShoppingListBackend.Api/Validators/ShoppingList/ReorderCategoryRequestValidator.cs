using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class ReorderCategoryRequestValidator : AbstractValidator<ReorderCategoryRequest>
{
    public ReorderCategoryRequestValidator()
    {
        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(0).WithMessage("Position must be non-negative.");
    }
}