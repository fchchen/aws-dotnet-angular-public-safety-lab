using PublicSafetyLab.Application.Exceptions;
using PublicSafetyLab.Domain.Exceptions;

namespace PublicSafetyLab.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainValidationException ex)
        {
            logger.LogWarning(ex, "Validation error");
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Validation failed", ex.Message);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Not found");
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = new
        {
            type = "about:blank",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
