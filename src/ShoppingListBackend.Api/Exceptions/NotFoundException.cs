namespace ShoppingListBackend.Api.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, "Not Found", 404) { }
}