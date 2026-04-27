using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryNameRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");
    }
}
