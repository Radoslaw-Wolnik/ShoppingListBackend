namespace ShoppingListBackend.Api.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message, string title, int statusCode) : base(message)
    {
        Title = title;
        StatusCode = statusCode;
    }

    public string Title { get; }
    public int StatusCode { get; }
    public virtual object? Extensions => null; // optional extra data
}