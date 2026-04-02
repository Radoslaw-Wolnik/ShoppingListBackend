using FluentValidation;
using ShoppingListBackend.Api.DTOs.ShoppingList;

namespace ShoppingListBackend.Api.Validators.ShoppingList;

public class AddEditorRequestValidator : AbstractValidator<AddEditorRequest>
{
    public AddEditorRequestValidator()
    {
        RuleFor(x => x.EditorDeviceId)
            .NotEmpty().WithMessage("Editor device ID is required.")
            .NotEqual(Guid.Empty).WithMessage("Editor device ID must be a valid GUID.");
    }
}