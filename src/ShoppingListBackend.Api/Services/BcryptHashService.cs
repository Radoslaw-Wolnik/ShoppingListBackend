using BC = BCrypt.Net.BCrypt;

namespace ShoppingListBackend.Api.Services;

public class BcryptHashService : IHashService
{
    public string Hash(string input) => BC.HashPassword(input);
    public bool Verify(string input, string hash) => BC.Verify(input, hash);
}