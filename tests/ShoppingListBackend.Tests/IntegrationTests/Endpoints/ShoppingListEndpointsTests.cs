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

    [Fact]
    public async Task CreateList_ReturnsCreated()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var request = new CreateListRequest { Title = "My List" };
        var response = await client.PostAsJsonAsync("/api/shopping-lists", request);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ((Guid)result!.Id).Should().NotBeEmpty();
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
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)createResult!.Id;

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
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)createResult!.Id;

        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fullList!.Id.Should().Be(listId);
        fullList.Title.Should().Be("My List");
        fullList.Owner.Id.Should().Be(device!.DeviceId);
        fullList.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateListTitle_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "Old Title" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)createResult!.Id;

        var updateRequest = new UpdateTitleRequest { Title = "New Title" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/{listId}/title", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task DeleteList_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createRequest = new CreateListRequest { Title = "To Delete" };
        var createResponse = await client.PostAsJsonAsync("/api/shopping-lists", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)createResult!.Id;

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
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

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
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "Old Name" };
        var addResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addResponse.Headers.Location!.Segments.Last();

        var updateRequest = new UpdateCategoryNameRequest { Name = "New Name" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/categories/{categoryId}", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories.Should().ContainSingle(c => c.Id == Guid.Parse(categoryId) && c.Name == "New Name");
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "To Delete" };
        var addResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addResponse.Headers.Location!.Segments.Last();

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
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addCategoryResponse.Headers.Location!.Segments.Last();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var response = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Description == "Apple");
    }

    [Fact]
    public async Task UpdateItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addCategoryResponse.Headers.Location!.Segments.Last();

        var addItemRequest = new AddItemRequest { Description = "Old Item" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = addItemResponse.Headers.Location!.Segments.Last();

        var updateRequest = new UpdateItemDescriptionRequest { Description = "New Item" };
        var updateResponse = await client.PutAsJsonAsync($"/api/shopping-lists/items/{itemId}", updateRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Id == Guid.Parse(itemId) && i.Description == "New Item");
    }

    [Fact]
    public async Task ToggleItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addCategoryResponse.Headers.Location!.Segments.Last();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = addItemResponse.Headers.Location!.Segments.Last();

        var toggleRequest = new ToggleItemRequest { IsChecked = true };
        var toggleResponse = await client.PutAsJsonAsync($"/api/shopping-lists/items/{itemId}/toggle", toggleRequest);
        var getResponse = await client.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Categories[0].Items.Should().ContainSingle(i => i.Id == Guid.Parse(itemId) && i.IsChecked == true);
    }

    [Fact]
    public async Task DeleteItem_ReturnsNoContent()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var createListRequest = new CreateListRequest { Title = "List" };
        var createListResponse = await client.PostAsJsonAsync("/api/shopping-lists", createListRequest);
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var addCategoryRequest = new AddCategoryRequest { Name = "Produce" };
        var addCategoryResponse = await client.PostAsJsonAsync($"/api/shopping-lists/{listId}/categories", addCategoryRequest);
        var categoryId = addCategoryResponse.Headers.Location!.Segments.Last();

        var addItemRequest = new AddItemRequest { Description = "Apple" };
        var addItemResponse = await client.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", addItemRequest);
        var itemId = addItemResponse.Headers.Location!.Segments.Last();

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
        var listResult = await createListResponse.Content.ReadFromJsonAsync<dynamic>();
        var listId = (Guid)listResult!.Id;

        var (editorClient, editorDevice) = await CreateAuthenticatedClient();
        var addEditorRequest = new AddEditorRequest { EditorDeviceId = editorDevice!.DeviceId };
        var response = await ownerClient.PostAsJsonAsync($"/api/shopping-lists/{listId}/editors", addEditorRequest);
        var getResponse = await editorClient.GetAsync($"/api/shopping-lists/{listId}");
        var fullList = await getResponse.Content.ReadFromJsonAsync<ShoppingListDto>();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fullList!.Editors.Should().Contain(e => e.Id == editorDevice.DeviceId);
    }
}