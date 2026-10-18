using Microsoft.AspNetCore.Antiforgery;

namespace SurubDirect.Identity.Api.Middleware;
public sealed class AntiforgeryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var request = context.Request;
        if (request.Path.StartsWithSegments("/api") && !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            await antiforgery.ValidateRequestAsync(context);
        await next(context);
    }
}
