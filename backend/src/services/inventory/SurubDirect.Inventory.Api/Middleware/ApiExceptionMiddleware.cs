using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.Inventory.Api.Middleware;
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BadHttpRequestException e)
        {
            await Results.Problem("Datele trimise nu sunt valide.", statusCode: e.StatusCode).ExecuteAsync(context);
        }
        catch (BusinessException e)
        {
            await Results.Problem(e.Message, statusCode: e.Status).ExecuteAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            await Results.Problem("Sesiunea formularului a expirat. Reîncarcă pagina.", statusCode: 400).ExecuteAsync(context);
        }
        catch (DbUpdateException e)when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await Results.Problem("Înregistrarea există deja.", statusCode: 409).ExecuteAsync(context);
        }
        catch (Exception e)when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Request failed");
            await Results.Problem("Operațiunea nu a putut fi finalizată.", statusCode: 500).ExecuteAsync(context);
        }
    }
}
