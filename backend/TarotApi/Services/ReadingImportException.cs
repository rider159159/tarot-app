namespace TarotApi.Services;

// Thrown by ReadingService.ImportReading on validation failures.
// Caught by ExceptionHandlingMiddleware and returned as 400 with the code.
public class ReadingImportException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}
