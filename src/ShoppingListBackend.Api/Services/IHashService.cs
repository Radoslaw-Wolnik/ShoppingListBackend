namespace ShoppingListBackend.Api.Services;

public interface IHashService
{
    string Hash(string input);
    bool Verify(string input, string hash);
}