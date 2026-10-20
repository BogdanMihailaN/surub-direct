using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context) { try { await next(context); } catch (BusinessException e) { await Results.Problem(e.Message, statusCode: e.Status).ExecuteAsync(context); } catch (BadHttpRequestException e) { await Results.Problem("Coș invalid.", statusCode: e.StatusCode).ExecuteAsync(context); } catch (Exception e) when (e is not OperationCanceledException) { logger.LogError(e, "Ordering request failed"); await Results.Problem("Coșul nu poate fi verificat momentan.", statusCode: 500).ExecuteAsync(context); } }
}
