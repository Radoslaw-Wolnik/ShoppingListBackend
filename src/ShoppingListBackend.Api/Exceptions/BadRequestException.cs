namespace ShoppingListBackend.Api.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message, "Bad Request", 400) { }
}