using SurubDirect.Ordering.Application;
namespace SurubDirect.Ordering.Api.Endpoints;
public static class GetDeliveryOptionsEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/ordering/delivery-options", (HttpContext context, GetDeliveryOptionsHandler handler, IWebHostEnvironment environment) =>
    {
        context.Response.Headers.CacheControl = "no-store";
        return environment.IsDevelopment() ? Results.Ok(handler.Handle()) : Results.NotFound();
    });
}
