using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class UpdateItemRequestValidator : AbstractValidator<UpdateItemDescriptionRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Item description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}
