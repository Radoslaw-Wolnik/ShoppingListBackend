using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class UpdateTitleRequestValidator : AbstractValidator<UpdateTitleRequest>
{
    public UpdateTitleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
    }
}
