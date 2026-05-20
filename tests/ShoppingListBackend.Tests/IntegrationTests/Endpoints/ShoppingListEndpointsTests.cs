using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.DTOs.ShoppingList;          // Contains CreateListRequest, AddCategoryRequest, etc.
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;
using ShoppingListBackend.Api.DTOs.ShoppingList.Response; // ShoppingListDto, DeviceShoppingListHeader
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.IntegrationTests.Endpoints;

public class ShoppingListEndpointsTests : IntegrationTestBase
{
    public ShoppingListEndpointsTests(WebApplicationFactory<Program> factory) : base(factory) { }

    private async Task<(HttpClient client, RegisterDeviceResponse device)> CreateAuthenticatedClient()
    {
        var registerResponse = await Client.PostAsync("/api/devices/register", null);
        var device = await registerResponse.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", device!.ApiKey);
        return (client, device);
    }

    private static async Task<Guid> CreateListAsync(HttpClient client, string title = "List")
    {
        var response = await client.PostAsJsonAsync("/api/shopping-lists", new CreateListRequest { Title = title });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadCreatedIdAsync();
    }

    private static async Task<Guid> AddCategoryAsync(HttpClient client, Guid listId, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", new AddCategoryRequest { Name = name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadCreatedIdAsync();
    }

    private static async Task<Guid> AddItemAsync(HttpClient client, Guid categoryId, string description)
    {
        var response = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", new AddItemRequest { Description = description });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.ReadCreatedIdAsync();
    }

    private static async Task<ShoppingListDto> GetListAsync(HttpClient client, Guid listId)
    {
        var response = await client.GetAsync($"/api/shopping-lists/{listId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ShoppingListDto>())!;
    }

    [Fact]
    public async Task GetListsForUser_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/shopping-lists");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateList_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var request = new CreateListRequest { Title = "My List" };
        var response = await client.PostAsJsonAsync("/api/shopping-lists", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var listId = await response.ReadCreatedIdAsync();
        listId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateList_WithBlankTitle_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/shopping-lists", new CreateListRequest { Title = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetListsForUser_ReturnsEmpty_WhenNoLists()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/shopping-lists");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lists = await response.Content.ReadFromJsonAsync<DeviceShoppingListHeader[]>();
        lists.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListsForUser_ReturnsCreatedList()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "My List" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var listId = await createResponse.ReadCreatedIdAsync();

        var getResponse = await client.GetAsync("/api/shopping-lists");
        var headers = await getResponse.Content.ReadFromJsonAsync<DeviceShoppingListHeader[]>();
        headers.Should().ContainSingle(h => h!.Id == listId && h.Title == "My List");
        headers![0]!.Owner.Id.Should().Be(device!.DeviceId);
    }

    [Fact]
    public async Task GetListById_ReturnsFullList()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "My List" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var listId = await createResponse.ReadCreatedIdAsync();

        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fullList!.Id.Should().Be(listId);
        fullList.Title.Should().Be("My List");
        fullList.Owner.Id.Should().Be(device!.DeviceId);
        fullList.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListById_WhenDeviceHasNoAccess_ReturnsNotFound()
    {
        var (ownerClient, _) = await CreateAuthenticatedClient();
        var (otherClient, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(ownerClient, "Private list");

        var response = await otherClient.GetAsync($"/api/shopping-lists/{listId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateListTitle_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "Old Title" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var listId = await createResponse.ReadCreatedIdAsync();

        var updateRequest = new UpdateTitleRequest { Title = "New Title" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/{listId}/title", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateListTitle_WhenEditor_ReturnsForbidden()
    {
        var (ownerClient, _) = await CreateAuthenticatedClient();
        var (editorClient, editorDevice) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(ownerClient, "Owner controlled");
        await ownerClient.PostAsJsonAsync($"/api/shopping-lists/{listId}/editors", new AddEditorRequest { EditorDeviceId = editorDevice.DeviceId });

        var response = await editorClient.PutAsJsonAsync($"/api/shopping-lists/{listId}/title", new UpdateTitleRequest { Title = "Editor edit" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteList_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "To Delete" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var listId = await createResponse.ReadCreatedIdAsync();

        var deleteResponse = await client.DeleteAsync($"/api/shopping-lists/{listId}");
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddCategory_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var response = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        fullList!.Categories.Should().ContainSingle(c => c.Name == "Produce");
    }

    [Fact]
    public async Task UpdateCategory_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Old Name" };
        var addResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addResponse.ReadCreatedIdAsync();

        var updateRequest = new UpdateCategoryNameRequest { Name = "New Name" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/categories/{categoryId}", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories.Should().ContainSingle(c => c.Id == categoryId && c.Name == "New Name");
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "To Delete" };
        var addResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addResponse.ReadCreatedIdAsync();

        var deleteResponse = await client.DeleteAsync($"/api/shopping-lists/categories/{categoryId}");
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItem_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addCategoryResponse.ReadCreatedIdAsync();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var response = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Description == "Apple");
    }

    [Fact]
    public async Task CopyList_ReturnsCopiedStructure_WithUncheckedItems()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(client, "Weekly");
        var categoryId = await AddCategoryAsync(client, listId, "Produce");
        var itemId = await AddItemAsync(client, categoryId, "Apples");
        await client.PutAsJsonAsync($"/api/shopping-lists/items/{itemId}/toggle", new ToggleItemRequest { IsChecked = true });

        var copyResponse = await client.PostAsync($"/api/shopping-lists/{listId}/copy", null);
        copyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var copiedListId = await copyResponse.ReadCreatedIdAsync();

        var copied = await GetListAsync(client, copiedListId);
        copied.Title.Should().Be("Weekly (copy)");
        copied.Categories.Should().ContainSingle(c => c.Name == "Produce");
        copied.Categories[0].Items.Should().ContainSingle(i => i.Description == "Apples" && !i.IsChecked);
    }

    [Fact]
    public async Task ReorderCategory_UpdatesCategoryPositions()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(client);
        var firstCategoryId = await AddCategoryAsync(client, listId, "First");
        await AddCategoryAsync(client, listId, "Second");
        await AddCategoryAsync(client, listId, "Third");

        var response = await client.PutAsJsonAsync(
            $"/api/shopping-lists/categories/{firstCategoryId}/reorder",
            new ReorderCategoryRequest { NewPosition = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var list = await GetListAsync(client, listId);
        list.Categories.Select(c => c.Name).Should().Equal("Second", "Third", "First");
        list.Categories.Select(c => c.Position).Should().Equal(0, 1, 2);
    }

    [Fact]
    public async Task ReorderItem_UpdatesItemPositions()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(client);
        var categoryId = await AddCategoryAsync(client, listId, "Main");
        var firstItemId = await AddItemAsync(client, categoryId, "First");
        await AddItemAsync(client, categoryId, "Second");
        await AddItemAsync(client, categoryId, "Third");

        var response = await client.PutAsJsonAsync(
            $"/api/shopping-lists/categories/{categoryId}/items/{firstItemId}/reorder",
            new ReorderItemRequest { NewPosition = 2 });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var list = await GetListAsync(client, listId);
        list.Categories[0].Items.Select(i => i.Description).Should().Equal("Second", "Third", "First");
        list.Categories[0].Items.Select(i => i.Position).Should().Equal(0, 1, 2);
    }

    [Fact]
    public async Task MoveItem_MovesItemBetweenCategories()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(client);
        var sourceCategoryId = await AddCategoryAsync(client, listId, "Source");
        var destinationCategoryId = await AddCategoryAsync(client, listId, "Destination");
        var itemId = await AddItemAsync(client, sourceCategoryId, "Milk");

        var response = await client.PutAsJsonAsync(
            $"/api/shopping-lists/items/{itemId}/move",
            new MoveItemRequest { NewCategoryId = destinationCategoryId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var list = await GetListAsync(client, listId);
        list.Categories.Single(c => c.Id == sourceCategoryId).Items.Should().BeEmpty();
        list.Categories.Single(c => c.Id == destinationCategoryId).Items.Should().ContainSingle(i => i.Id == itemId);
    }

    [Fact]
    public async Task ResetCheckedItems_ClearsCheckedItems()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(client);
        var categoryId = await AddCategoryAsync(client, listId, "Main");
        var checkedItemId = await AddItemAsync(client, categoryId, "Checked");
        await AddItemAsync(client, categoryId, "Open");
        await client.PutAsJsonAsync($"/api/shopping-lists/items/{checkedItemId}/toggle", new ToggleItemRequest { IsChecked = true });

        var response = await client.PostAsync($"/api/shopping-lists/{listId}/reset-checked", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var list = await GetListAsync(client, listId);
        list.Categories[0].Items.Should().OnlyContain(i => !i.IsChecked);
    }

    [Fact]
    public async Task UpdateItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addCategoryResponse.ReadCreatedIdAsync();

        var addItemRequest = new AddItemRequest { Description = "Old Item" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = await addItemResponse.ReadCreatedIdAsync();

        var updateRequest = new UpdateItemDescriptionRequest { Description = "New Item" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/items/{itemId}", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Id == itemId && i.Description == "New Item");
    }

    [Fact]
    public async Task ToggleItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addCategoryResponse.ReadCreatedIdAsync();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = await addItemResponse.ReadCreatedIdAsync();

        var toggleRequest = new ToggleItemRequest { IsChecked = true };
        var toggleResponse = await client.PutAsJsonAsync($"/api/shopping-lists/items/{itemId}/toggle", toggleRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Id == itemId && i.IsChecked == true);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = await addCategoryResponse.ReadCreatedIdAsync();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = await addItemResponse.ReadCreatedIdAsync();

        var deleteResponse = await client.DeleteAsync($"/api/shopping-lists/items/{itemId}");
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories[0].Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddEditor_WhenOwner_ReturnsNoContent()
    {
        var (ownerClient, ownerDevice) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "Shared List" };
        var createListResponse = await ownerClient.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listId = await createListResponse.ReadCreatedIdAsync();

        var (editorClient, editorDevice) = await CreateAuthenticatedClient();
        var addEditorRequest = new AddEditorRequest { EditorDeviceId = editorDevice!.DeviceId };
        var response = await ownerClient.PostAsJsonAsync($"/api/shopping-lists/{listId}/editors", addEditorRequest);
        var getResponse = await editorClient.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Editors.Should().Contain(e => e.Id == editorDevice.DeviceId);
    }

    [Fact]
    public async Task RemoveEditor_RevokesListAccess()
    {
        var (ownerClient, _) = await CreateAuthenticatedClient();
        var (editorClient, editorDevice) = await CreateAuthenticatedClient();
        var listId = await CreateListAsync(ownerClient, "Shared List");
        await ownerClient.PostAsJsonAsync($"/api/shopping-lists/{listId}/editors", new AddEditorRequest { EditorDeviceId = editorDevice.DeviceId });

        var removeResponse = await ownerClient.DeleteAsync($"/api/shopping-lists/{listId}/editors/{editorDevice.DeviceId}");
        var editorReadResponse = await editorClient.GetAsync($"/api/shopping-lists/{listId}");

        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        editorReadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
