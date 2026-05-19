using System.Net.Http.Json;
using System.Text.Json;

namespace ShoppingListBackend.Tests.Helpers;

public static class HttpJsonTestExtensions
{
    public static async Task<Guid> ReadCreatedIdAsync(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }
}
