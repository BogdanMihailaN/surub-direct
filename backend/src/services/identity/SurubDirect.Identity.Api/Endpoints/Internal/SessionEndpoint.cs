using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using SurubDirect.ServiceDefaults.Security;

namespace SurubDirect.Identity.Api.Endpoints;
public static class SessionEndpoint
{
    public static void Map(IEndpointRouteBuilder routes, IConfiguration configuration)
    {
        var key = ServiceKey.Required(configuration, "ServiceKeys:Identity");
        routes.MapPost("/internal/session", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!ServiceKey.Matches(context.Request, key) || context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();
            var method = context.Request.Headers["X-Original-Method"].ToString();
            if (string.IsNullOrEmpty(method))
                return Results.BadRequest();
            if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
                await antiforgery.ValidateRequestAsync(context);
            return Results.Ok(new SessionPrincipal(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!, context.User.Identity.Name ?? "", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()));
        });
    }
}
