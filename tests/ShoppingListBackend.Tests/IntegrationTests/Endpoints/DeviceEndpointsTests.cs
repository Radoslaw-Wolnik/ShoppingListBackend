using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Tests.Helpers;
using Xunit;

namespace ShoppingListBackend.Tests.IntegrationTests.Endpoints;

public class DeviceEndpointsTests : IntegrationTestBase
{
    public DeviceEndpointsTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task RegisterDevice_ReturnsApiKey()
    {
        var response = await Client.PostAsync("/api/devices/register", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        result.Should().NotBeNull();
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.DeviceId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMe_ReturnsDeviceInfo_WhenAuthenticated()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/devices/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<DeviceInfo>();
        me!.Id.Should().Be(device.DeviceId);
    }

    [Fact]
    public async Task UpdateUsername_UpdatesSuccessfully()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var newName = "NewNick";
        var updateResponse = await client.PutAsJsonAsync("/api/devices/me/username", new UpdateUsernameRequest { UserName = newName });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/devices/me");
        var me = await getResponse.Content.ReadFromJsonAsync<DeviceInfo>();
        me!.UserName.Should().Be(newName);
    }

    [Fact]
    public async Task UpdateColour_UpdatesSuccessfully()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var newColour = "#00FFAA";
        var updateResponse = await client.PutAsJsonAsync("/api/devices/me/colour", new UpdateColourRequest { Colour = newColour });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/devices/me");
        var me = await getResponse.Content.ReadFromJsonAsync<DeviceInfo>();
        me!.Colour.Should().Be(newColour);
    }

    [Fact]
    public async Task UpdateColour_WithInvalidHex_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var response = await client.PutAsJsonAsync("/api/devices/me/colour", new UpdateColourRequest { Colour = "blue" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddFriend_And_ListFriends_Work()
    {
        var (client1, device1) = await CreateAuthenticatedClient();
        var (client2, device2) = await CreateAuthenticatedClient();

        var addResponse = await client1.PostAsJsonAsync("/api/devices/me/friends", new AddFriendRequest { FriendDeviceId = device2.DeviceId });
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getFriends = await client1.GetAsync("/api/devices/me/friends");
        var friends = await getFriends.Content.ReadFromJsonAsync<FriendDto[]>();
        friends.Should().Contain(f => f.Id == device2.DeviceId);
    }

    [Fact]
    public async Task DeleteDevice_RemovesDevice()
    {
        var (client, device) = await CreateAuthenticatedClient();
        var deleteResponse = await client.DeleteAsync("/api/devices/me");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Try to get device info – should fail
        var getResponse = await client.GetAsync("/api/devices/me");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // or 404 depending on auth
    }

    private async Task<(HttpClient client, RegisterDeviceResponse device)> CreateAuthenticatedClient()
    {
        var registerResponse = await Client.PostAsync("/api/devices/register", null);
        var device = await registerResponse.Content.ReadFromJsonAsync<RegisterDeviceResponse>();
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", device!.ApiKey);
        return (client, device);
    }
}
