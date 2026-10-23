using System.Security.Claims;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Api.Endpoints;
public static class ReplayMessageEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/api/v1/admin/messaging/{id:guid}/replay", async (Guid id, RequestMessageReplay request, HttpContext context, ReplayMessageHandler handler, IConfiguration config, CancellationToken ct) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!config.GetValue<bool>("Messaging:Enabled"))
            throw new SurubDirect.BuildingBlocks.BusinessException("Mesageria nu este configurată.", 503);
        var actor = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor))
            return Results.Unauthorized();
        return Results.Ok(await handler.Handle(id, request, actor, ct));
    }).RequireAuthorization("Admin");
}
