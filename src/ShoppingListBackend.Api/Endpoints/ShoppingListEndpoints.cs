using System.Security.Claims;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Endpoints;

public static class ShoppingListEndpoints
{
    public static void MapShoppingListEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shopping-lists").WithTags("ShoppingLists").RequireAuthorization();

        // List management
        group.MapGet("/", GetListsForUser);
        group.MapGet("/{id:guid}", GetListById);
        group.MapPost("/", CreateList);
        group.MapPut("/{id:guid}/title", UpdateListTitle);
        group.MapDelete("/{id:guid}", DeleteList);
        group.MapPost("/{id:guid}/copy", CopyList);
        group.MapPost("/{id:guid}/reset-checked", ResetCheckedItems);

        // Editors
        group.MapPost("/{id:guid}/editors", AddEditor);
        group.MapDelete("/{id:guid}/editors/{editorId:guid}", RemoveEditor);

        // Categories
        group.MapPost("/{listId:guid}/categories", AddCategory);
        group.MapPut("/categories/{categoryId:guid}", UpdateCategory);
        group.MapDelete("/categories/{categoryId:guid}", DeleteCategory);
        group.MapPut("/categories/{categoryId:guid}/reorder", ReorderCategory);

        // Items
        group.MapPost("/categories/{categoryId:guid}/items", AddItem);
        group.MapPut("/items/{itemId:guid}", UpdateItem);
        group.MapPut("/items/{itemId:guid}/toggle", ToggleItem);
        group.MapDelete("/items/{itemId:guid}", DeleteItem);
        group.MapPut("/categories/{categoryId:guid}/items/{itemId:guid}/reorder", ReorderItem);
        group.MapPut("/items/{itemId:guid}/move", MoveItem);
    }

    // --- Handlers ---
    private static async Task<IResult> GetListsForUser(
        HttpContext httpContext,
        ShoppingListReadService readService)
    {
        var deviceId = GetDeviceId(httpContext);
        var summaries = await readService.GetSummariesForDeviceAsync(deviceId);
        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetListById(
        Guid id,
        HttpContext httpContext,
        ShoppingListReadService readService)
    {
        var deviceId = GetDeviceId(httpContext);
        var fullList = await readService.GetHydratedListAsync(id, deviceId);
        return fullList != null ? Results.Ok(fullList) : Results.NotFound();
    }

    private static async Task<IResult> CreateList(
        HttpContext httpContext,
        IShoppingListService listService,
        CreateListRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        var list = await listService.CreateListAsync(deviceId, request.Title);
        return Results.Created($"/api/shopping-lists/{list.Id}", new { list.Id });
    }

    private static async Task<IResult> UpdateListTitle(
        Guid id,
        HttpContext httpContext,
        IShoppingListService listService,
        UpdateTitleRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.UpdateListTitleAsync(id, deviceId, request.Title);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteList(
        Guid id,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.DeleteListAsync(id, deviceId);
        return Results.NoContent();
    }

    private static async Task<IResult> CopyList(
        Guid id,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        var newList = await listService.CopyListAsync(id, deviceId);
        return Results.Created($"/api/shopping-lists/{newList.Id}", new { newList.Id });
    }

    private static async Task<IResult> ResetCheckedItems(
        Guid id,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.ResetCheckedItemsAsync(id, deviceId);
        return Results.NoContent();
    }

    // Editor handlers
    private static async Task<IResult> AddEditor(
        Guid id,
        HttpContext httpContext,
        IShoppingListService listService,
        AddEditorRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.AddEditorAsync(id, deviceId, request.EditorDeviceId);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveEditor(
        Guid id,
        Guid editorId,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.RemoveEditorAsync(id, deviceId, editorId);
        return Results.NoContent();
    }

    // Category handlers
    private static async Task<IResult> AddCategory(
        Guid listId,
        HttpContext httpContext,
        IShoppingListService listService,
        AddCategoryRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.AddCategoryAsync(listId, deviceId, request.Name);
        return Results.Created($"/api/shopping-lists/{listId}/categories", null);
    }

    private static async Task<IResult> UpdateCategory(
        Guid categoryId,
        HttpContext httpContext,
        IShoppingListService listService,
        UpdateCategoryNameRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.UpdateCategoryNameAsync(categoryId, deviceId, request.Name);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCategory(
        Guid categoryId,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.DeleteCategoryAsync(categoryId, deviceId);
        return Results.NoContent();
    }

    private static async Task<IResult> ReorderCategory(
        Guid categoryId,
        HttpContext httpContext,
        IShoppingListService listService,
        ReorderCategoryRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        // This uses the modified service method that accepts categoryId directly
        await listService.ReorderCategoryAsync(categoryId, deviceId, request.NewPosition);
        return Results.NoContent();
    }

    // Item handlers
    private static async Task<IResult> AddItem(
        Guid categoryId,
        HttpContext httpContext,
        IShoppingListService listService,
        AddItemRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.AddItemAsync(categoryId, deviceId, request.Description);
        return Results.Created($"/api/categories/{categoryId}/items", null);
    }

    private static async Task<IResult> UpdateItem(
        Guid itemId,
        HttpContext httpContext,
        IShoppingListService listService,
        UpdateItemDescriptionRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.UpdateItemDescriptionAsync(itemId, deviceId, request.Description);
        return Results.NoContent();
    }

    private static async Task<IResult> ToggleItem(
        Guid itemId,
        HttpContext httpContext,
        IShoppingListService listService,
        ToggleItemRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.ToggleItemCheckedAsync(itemId, deviceId, request.IsChecked);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteItem(
        Guid itemId,
        HttpContext httpContext,
        IShoppingListService listService)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.DeleteItemAsync(itemId, deviceId);
        return Results.NoContent();
    }

    private static async Task<IResult> ReorderItem(
        Guid categoryId,
        Guid itemId,
        HttpContext httpContext,
        IShoppingListService listService,
        ReorderItemRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.ReorderItemAsync(categoryId, deviceId, itemId, request.NewPosition);
        return Results.NoContent();
    }

    private static async Task<IResult> MoveItem(
        Guid itemId,
        HttpContext httpContext,
        IShoppingListService listService,
        MoveItemRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await listService.MoveItemToCategoryAsync(itemId, deviceId, request.NewCategoryId);
        return Results.NoContent();
    }

    private static Guid GetDeviceId(HttpContext httpContext)
    {
        var idClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !Guid.TryParse(idClaim, out var deviceId))
            throw new UnauthorizedAccessException("Device ID not found in claims");
        return deviceId;
    }
}