using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetPublicImageEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/api/v1/products/{id:guid}/images/{imageId:guid}/{size}",
        async (Guid id, Guid imageId, string size, HttpResponse response, GetImageHandler handler, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-store";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.Stream(await handler.Handle(id, imageId, size, false, ct), "image/webp");
        });
}
