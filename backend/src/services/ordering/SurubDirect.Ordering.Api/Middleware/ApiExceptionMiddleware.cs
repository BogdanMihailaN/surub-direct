using Microsoft.AspNetCore.Antiforgery;
using Npgsql;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Api.Middleware;
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AntiforgeryValidationException)
        {
            await Results.Problem("Reîncarcă pagina înainte de a modifica coșul.", statusCode: 400).ExecuteAsync(context);
        }
        catch (NpgsqlException e)
        {
            logger.LogError(e, "Ordering database unavailable");
            await Results.Problem("Datele solicitate nu sunt disponibile momentan.", statusCode: 503).ExecuteAsync(context);
        }
        catch (BusinessException e)
        {
            await Results.Problem(e.Message, statusCode: e.Status).ExecuteAsync(context);
        }
        catch (BadHttpRequestException e)
        {
            await Results.Problem("Cerere invalidă.", statusCode: e.StatusCode).ExecuteAsync(context);
        }
        catch (Exception e)when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Ordering request failed");
            await Results.Problem("Operația nu poate fi procesată momentan.", statusCode: 500).ExecuteAsync(context);
        }
    }
}
