using BCrypt.Net;

namespace ShoppingListBackend.Api.Services;

public class BcryptHashService : IHashService
{
    public string Hash(string input) => BCrypt.HashPassword(input);
    public bool Verify(string input, string hash) => BCrypt.Verify(input, hash);
}