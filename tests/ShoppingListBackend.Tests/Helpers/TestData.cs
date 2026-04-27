using ShoppingListBackend.Api.Models;
using System;
using System.Collections.Generic;

namespace ShoppingListBackend.Tests.Helpers;

public static class TestData
{
    private static int _counter = 0;

    public static Device CreateDevice(Guid? id = null, string? username = null, string? colour = null)
    {
        var deviceId = id ?? Guid.NewGuid();
        return new Device
        {
            Id = deviceId,
            UserName = username ?? $"User{_counter++}",
            Colour = colour ?? "#" + (deviceId.ToString().Substring(0, 6)),
            CreatedAt = DateTime.UtcNow,
            ApiKeyHash = "hashed",
            ApiKeySha256 = "sha256"
        };
    }

    public static ShoppingList CreateShoppingList(Guid? id = null, Guid? ownerId = null, string? title = null)
    {
        return new ShoppingList
        {
            Id = id ?? Guid.NewGuid(),
            Title = title ?? $"List{_counter++}",
            OwnerDeviceId = ownerId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Categories = new List<ShoppingListCategory>()
        };
    }

    public static ShoppingListCategory CreateCategory(Guid listId, int position, string? name = null)
    {
        return new ShoppingListCategory
        {
            Id = Guid.NewGuid(),
            Name = name ?? $"Category{_counter++}",
            Position = position,
            ShoppingListId = listId,
            Items = new List<ShoppingListItem>()
        };
    }

    public static ShoppingListItem CreateItem(Guid categoryId, int position, string? description = null, bool isChecked = false)
    {
        return new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            Description = description ?? $"Item{_counter++}",
            IsChecked = isChecked,
            Position = position,
            ShoppingListCategoryId = categoryId
        };
    }
}