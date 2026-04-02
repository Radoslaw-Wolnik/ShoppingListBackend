using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class CreateListRequestValidator : AbstractValidator<CreateListRequest>
{
    public CreateListRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("List title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
    }
}