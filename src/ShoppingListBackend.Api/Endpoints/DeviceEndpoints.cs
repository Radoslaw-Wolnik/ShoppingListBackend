using System.Security.Claims;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.Extensions;
using ShoppingListBackend.Api.Services;

namespace ShoppingListBackend.Api.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices").WithTags("Devices");

        // Public endpoint for registration
        group.MapPost("/register", RegisterDevice).AllowAnonymous();

        // Authenticated endpoints
        group.MapGet("/me", GetMyDevice).RequireAuthorization();
        group.MapPut("/me/username", UpdateUsername)
            .RequireAuthorization()
            .WithRequestValidation<UpdateUsernameRequest>();
        group.MapPut("/me/colour", UpdateColour)
            .RequireAuthorization()
            .WithRequestValidation<UpdateColourRequest>();
        group.MapPost("/me/friends", AddFriend)
            .RequireAuthorization()
            .WithRequestValidation<AddFriendRequest>();
        group.MapDelete("/me/friends/{friendId:guid}", RemoveFriend).RequireAuthorization();
        group.MapGet("/me/friends", GetFriends).RequireAuthorization();
        group.MapDelete("/me", DeleteDevice).RequireAuthorization();
    }

    private static async Task<IResult> RegisterDevice(IAuthService authService)
    {
        var response = await authService.RegisterDeviceAsync();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetMyDevice(
        HttpContext httpContext,
        IDeviceService deviceService)
    {
        var deviceId = GetDeviceId(httpContext);
        var device = await deviceService.GetDeviceAsync(deviceId);
        return Results.Ok(new DeviceInfo
        {
            Id = device.Id,
            UserName = device.UserName,
            Colour = device.Colour
        });
    }

    private static async Task<IResult> UpdateUsername(
        HttpContext httpContext,
        IDeviceService deviceService,
        UpdateUsernameRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await deviceService.UpdateUsernameAsync(deviceId, request.UserName);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateColour(
        HttpContext httpContext,
        IDeviceService deviceService,
        UpdateColourRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await deviceService.UpdateColourAsync(deviceId, request.Colour);
        return Results.NoContent();
    }

    private static async Task<IResult> AddFriend(
        HttpContext httpContext,
        IDeviceService deviceService,
        AddFriendRequest request)
    {
        var deviceId = GetDeviceId(httpContext);
        await deviceService.AddFriendAsync(deviceId, request.FriendDeviceId);
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFriend(
        HttpContext httpContext,
        IDeviceService deviceService,
        Guid friendId)
    {
        var deviceId = GetDeviceId(httpContext);
        await deviceService.RemoveFriendAsync(deviceId, friendId);
        return Results.NoContent();
    }

    private static async Task<IResult> GetFriends(
        HttpContext httpContext,
        IDeviceService deviceService)
    {
        var deviceId = GetDeviceId(httpContext);
        var friends = await deviceService.GetFriendsAsync(deviceId);
        var friendDtos = friends.Select(f => new FriendDto
        {
            Id = f.Id,
            UserName = f.UserName,
            Colour = f.Colour
        });
        return Results.Ok(friendDtos);
    }

    private static async Task<IResult> DeleteDevice(
        HttpContext httpContext,
        IDeviceService deviceService)
    {
        var deviceId = GetDeviceId(httpContext);
        await deviceService.DeleteDeviceAsync(deviceId);
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
