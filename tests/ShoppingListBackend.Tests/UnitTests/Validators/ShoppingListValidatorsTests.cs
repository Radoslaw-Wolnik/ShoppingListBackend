using FluentAssertions;
using FluentValidation.TestHelper;
using ShoppingListBackend.Api.DTOs.ShoppingList;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;
using ShoppingListBackend.Api.Validators.ShoppingList;
using Xunit;

namespace ShoppingListBackend.Tests.UnitTests.Validators;

public class ShoppingListValidatorsTests
{
    private readonly CreateListRequestValidator _createListValidator = new();
    private readonly UpdateTitleRequestValidator _updateTitleValidator = new();
    private readonly AddCategoryRequestValidator _addCategoryValidator = new();
    private readonly UpdateCategoryRequestValidator _updateCategoryValidator = new();
    private readonly ReorderCategoryRequestValidator _reorderCategoryValidator = new();
    private readonly AddItemRequestValidator _addItemValidator = new();
    private readonly UpdateItemRequestValidator _updateItemValidator = new();
    private readonly ToggleItemRequestValidator _toggleItemValidator = new();
    private readonly ReorderItemRequestValidator _reorderItemValidator = new();
    private readonly MoveItemRequestValidator _moveItemValidator = new();
    private readonly AddEditorRequestValidator _addEditorValidator = new();

    [Fact]
    public void CreateListRequestValidator_ShouldPass_WhenTitleValid()
    {
        var request = new CreateListRequest { Title = "My List" };
        var result = _createListValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateListRequestValidator_ShouldFail_WhenTitleEmpty()
    {
        var request = new CreateListRequest { Title = "" };
        var result = _createListValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("List title is required.");
    }

    [Fact]
    public void CreateListRequestValidator_ShouldFail_WhenTitleTooLong()
    {
        var request = new CreateListRequest { Title = new string('a', 201) };
        var result = _createListValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void UpdateTitleRequestValidator_ShouldPass_WhenTitleValid()
    {
        var request = new UpdateTitleRequest { Title = "New Title" };
        var result = _updateTitleValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateTitleRequestValidator_ShouldFail_WhenTitleEmpty()
    {
        var request = new UpdateTitleRequest { Title = "" };
        var result = _updateTitleValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void UpdateTitleRequestValidator_ShouldFail_WhenTitleTooLong()
    {
        var request = new UpdateTitleRequest { Title = new string('a', 201) };
        var result = _updateTitleValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void AddCategoryRequestValidator_ShouldPass_WhenNameValid()
    {
        var request = new AddCategoryRequest { Name = "Produce" };
        var result = _addCategoryValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddCategoryRequestValidator_ShouldFail_WhenNameEmpty()
    {
        var request = new AddCategoryRequest { Name = "" };
        var result = _addCategoryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name is required.");
    }

    [Fact]
    public void AddCategoryRequestValidator_ShouldFail_WhenNameTooLong()
    {
        var request = new AddCategoryRequest { Name = new string('a', 101) };
        var result = _addCategoryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name cannot exceed 100 characters.");
    }

    [Fact]
    public void UpdateCategoryRequestValidator_ShouldPass_WhenNameValid()
    {
        var request = new UpdateCategoryNameRequest { Name = "Updated Name" };
        var result = _updateCategoryValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateCategoryRequestValidator_ShouldFail_WhenNameEmpty()
    {
        var request = new UpdateCategoryNameRequest { Name = "" };
        var result = _updateCategoryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name is required.");
    }

    [Fact]
    public void UpdateCategoryRequestValidator_ShouldFail_WhenNameTooLong()
    {
        var request = new UpdateCategoryNameRequest { Name = new string('a', 101) };
        var result = _updateCategoryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name cannot exceed 100 characters.");
    }

    [Fact]
    public void ReorderCategoryRequestValidator_ShouldPass_WhenPositionValid()
    {
        var request = new ReorderCategoryRequest { NewPosition = 5 };
        var result = _reorderCategoryValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReorderCategoryRequestValidator_ShouldFail_WhenPositionNegative()
    {
        var request = new ReorderCategoryRequest { NewPosition = -1 };
        var result = _reorderCategoryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPosition)
            .WithErrorMessage("Position must be non-negative.");
    }

    [Fact]
    public void AddItemRequestValidator_ShouldPass_WhenDescriptionValid()
    {
        var request = new AddItemRequest { Description = "Milk" };
        var result = _addItemValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddItemRequestValidator_ShouldFail_WhenDescriptionEmpty()
    {
        var request = new AddItemRequest { Description = "" };
        var result = _addItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Item description is required.");
    }

    [Fact]
    public void AddItemRequestValidator_ShouldFail_WhenDescriptionTooLong()
    {
        var request = new AddItemRequest { Description = new string('a', 501) };
        var result = _addItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void UpdateItemRequestValidator_ShouldPass_WhenDescriptionValid()
    {
        var request = new UpdateItemDescriptionRequest { Description = "Almond milk" };
        var result = _updateItemValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateItemRequestValidator_ShouldFail_WhenDescriptionEmpty()
    {
        var request = new UpdateItemDescriptionRequest { Description = "" };
        var result = _updateItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Item description is required.");
    }

    [Fact]
    public void UpdateItemRequestValidator_ShouldFail_WhenDescriptionTooLong()
    {
        var request = new UpdateItemDescriptionRequest { Description = new string('a', 501) };
        var result = _updateItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void ToggleItemRequestValidator_ShouldPass_Always()
    {
        var request = new ToggleItemRequest { IsChecked = true };
        var result = _toggleItemValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReorderItemRequestValidator_ShouldPass_WhenPositionValid()
    {
        var request = new ReorderItemRequest { NewPosition = 3 };
        var result = _reorderItemValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReorderItemRequestValidator_ShouldFail_WhenPositionNegative()
    {
        var request = new ReorderItemRequest { NewPosition = -1 };
        var result = _reorderItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewPosition)
            .WithErrorMessage("Position must be non-negative.");
    }

    [Fact]
    public void MoveItemRequestValidator_ShouldPass_WhenCategoryIdValid()
    {
        var request = new MoveItemRequest { NewCategoryId = Guid.NewGuid() };
        var result = _moveItemValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MoveItemRequestValidator_ShouldFail_WhenCategoryIdEmpty()
    {
        var request = new MoveItemRequest { NewCategoryId = Guid.Empty };
        var result = _moveItemValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.NewCategoryId)
            .WithErrorMessage("New category ID must be a valid GUID.");
    }

    [Fact]
    public void AddEditorRequestValidator_ShouldPass_WhenEditorIdValid()
    {
        var request = new AddEditorRequest { EditorDeviceId = Guid.NewGuid() };
        var result = _addEditorValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AddEditorRequestValidator_ShouldFail_WhenEditorIdEmpty()
    {
        var request = new AddEditorRequest { EditorDeviceId = Guid.Empty };
        var result = _addEditorValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EditorDeviceId)
            .WithErrorMessage("Editor device ID must be a valid GUID.");
    }
}