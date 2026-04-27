using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.DTOs.RealTime;
using ShoppingListBackend.Api.DTOs.ShoppingList.Request;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.IntegrationTests.SignalR;

public class ShoppingListHubIntegrationTests : IntegrationTestBase, IAsyncLifetime
{
    private HubConnection? _hubConnection;
    private Guid _listId;
    private RegisterDeviceResponse? _device;

    public ShoppingListHubIntegrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

    public async Task InitializeAsync()
    {
        // Register device
        var registerResponse = await Client.PostAsync("/api/devices/register", null);
        _device = await registerResponse.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        if (_device is null) throw new Exception("Device registration failed");

        // Create a list via HTTP
        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", _device.ApiKey);
        var createResponse = await httpClient.PostAsJsonAsync("/api/shopping-lists", new CreateListRequest { Title = "Test List" });
        var listResult = await createResponse.Content.ReadFromJsonAsync<dynamic>();
        _listId = (Guid)listResult!.Id;

        // Build SignalR connection
        var hubUrl = new Uri(Factory.Server.BaseAddress, "/hub/shoppingLists");
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Headers.Add("X-API-Key", _device.ApiKey);
            })
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task JoinList_Should_AddToGroup_And_BroadcastCurrentlyEditing()
    {
        var editingEventReceived = new TaskCompletionSource<CurrentlyEditingChangedEvent>();
        _hubConnection!.On<CurrentlyEditingChangedEvent>("CurrentlyEditingChanged", ev => editingEventReceived.TrySetResult(ev));

        await _hubConnection!.InvokeAsync("JoinList", _listId);

        var editingEvent = await editingEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        editingEvent.Should().NotBeNull();
        editingEvent.ListId.Should().Be(_listId);
        editingEvent.EditingDevices.Should().Contain(d => d.Id == _device!.DeviceId);
    }

    [Fact]
    public async Task AddItem_Should_Broadcast_ItemAddedEvent()
    {
        var itemAddedEventReceived = new TaskCompletionSource<ItemAddedEvent>();
        _hubConnection!.On<ItemAddedEvent>("ShoppingListEvent", ev =>
        {
            if (ev is ItemAddedEvent itemAdded)
                itemAddedEventReceived.TrySetResult(itemAdded);
        });

        await _hubConnection!.InvokeAsync("JoinList", _listId);

        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", _device!.ApiKey);
        var addCategoryResponse = await httpClient.PostAsJsonAsync($"/api/shopping-lists/{_listId}/categories", new AddCategoryRequest { Name = "Produce" });
        var categoryId = Guid.Parse(addCategoryResponse.Headers.Location!.Segments.Last());

        await _hubConnection!.InvokeAsync("AddItem", categoryId, "Apple");

        var itemAdded = await itemAddedEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        itemAdded.Should().NotBeNull();
        itemAdded.ListId.Should().Be(_listId);
        itemAdded.Item.Description.Should().Be("Apple");
        itemAdded.Item.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task ToggleItem_Should_Broadcast_ItemToggledEvent()
    {
        var toggledEventReceived = new TaskCompletionSource<ItemToggledEvent>();
        _hubConnection!.On<ItemToggledEvent>("ShoppingListEvent", ev =>
        {
            if (ev is ItemToggledEvent toggled)
                toggledEventReceived.TrySetResult(toggled);
        });

        await _hubConnection!.InvokeAsync("JoinList", _listId);

        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", _device!.ApiKey);
        var addCategoryResponse = await httpClient.PostAsJsonAsync($"/api/shopping-lists/{_listId}/categories", new AddCategoryRequest { Name = "Produce" });
        var categoryId = Guid.Parse(addCategoryResponse.Headers.Location!.Segments.Last());

        var addItemResponse = await httpClient.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", new AddItemRequest { Description = "Apple" });
        var itemId = Guid.Parse(addItemResponse.Headers.Location!.Segments.Last());

        await _hubConnection!.InvokeAsync("ToggleItem", itemId, true);

        var toggled = await toggledEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        toggled.Should().NotBeNull();
        toggled.ListId.Should().Be(_listId);
        toggled.ItemId.Should().Be(itemId);
        toggled.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteItem_Should_Broadcast_ItemDeletedEvent()
    {
        var deletedEventReceived = new TaskCompletionSource<ItemDeletedEvent>();
        _hubConnection!.On<ItemDeletedEvent>("ShoppingListEvent", ev =>
        {
            if (ev is ItemDeletedEvent deleted)
                deletedEventReceived.TrySetResult(deleted);
        });

        await _hubConnection!.InvokeAsync("JoinList", _listId);

        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", _device!.ApiKey);
        var addCategoryResponse = await httpClient.PostAsJsonAsync($"/api/shopping-lists/{_listId}/categories", new AddCategoryRequest { Name = "Produce" });
        var categoryId = Guid.Parse(addCategoryResponse.Headers.Location!.Segments.Last());

        var addItemResponse = await httpClient.PostAsJsonAsync($"/api/shopping-lists/categories/{categoryId}/items", new AddItemRequest { Description = "Apple" });
        var itemId = Guid.Parse(addItemResponse.Headers.Location!.Segments.Last());

        await _hubConnection!.InvokeAsync("DeleteItem", itemId);

        var deleted = await deletedEventReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        deleted.Should().NotBeNull();
        deleted.ListId.Should().Be(_listId);
        deleted.ItemId.Should().Be(itemId);
    }

    [Fact]
    public async Task Unauthorized_Connection_Should_Be_Rejected()
    {
        var invalidHubUrl = new Uri(Factory.Server.BaseAddress, "/hub/shoppingLists");
        var invalidConnection = new HubConnectionBuilder()
            .WithUrl(invalidHubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            })
            .Build();

        Func<Task> act = () => invalidConnection.StartAsync();
        await act.Should().ThrowAsync<HubException>().WithMessage("*unauthorized*");
    }

    [Fact]
    public async Task JoinList_Without_Access_Should_Throw_HubException()
    {
        var registerResponse2 = await Client.PostAsync("/api/devices/register", null);
        var device2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        if (device2 is null) throw new Exception("Second device registration failed");

        var hubUrl = new Uri(Factory.Server.BaseAddress, "/hub/shoppingLists");
        var hub2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Headers.Add("X-API-Key", device2.ApiKey);
            })
            .Build();
        await hub2.StartAsync();

        Func<Task> act = () => hub2.InvokeAsync("JoinList", _listId);
        await act.Should().ThrowAsync<HubException>().WithMessage("*access*");

        await hub2.DisposeAsync();
    }
}