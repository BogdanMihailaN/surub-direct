using SurubDirect.Catalog.Application;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class ArrangeImagesEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPut("/products/{id:guid}/images/order",
        async (Guid id, ArrangeImages command, ArrangeImagesHandler handler, CancellationToken ct) =>
            Results.Ok(new { id = await handler.Handle(id, command, ct) }));
}
