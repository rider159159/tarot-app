using System.Text.Json;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized, ex.Message, "UNAUTHORIZED");
        }
        catch (ReadingImportException ex)
        {
            logger.LogWarning(ex, "Reading import validation failed: {Code}", ex.Code);
            var status = ex.Code == "AUTH_REQUIRED"
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest;
            await WriteErrorResponse(context, status, ex.Message, ex.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "伺服器內部錯誤", "INTERNAL_ERROR");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string error, string code)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = new ErrorResponseDto { Error = error, Code = code };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
