namespace ShoppingListBackend.Api.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(message, "Forbidden", 403) { }
}