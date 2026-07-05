namespace CapitalPos.Api.Endpoints;

public sealed record ErrorResponse(string Message)
{
    public static ErrorResponse From(string message)
    {
        return new ErrorResponse(message);
    }
}
