using SurubDirect.Pricing.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class SetPriceEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/offers/{id:guid}/price", async (Guid id, SetPrice command, SetOfferPriceHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(id, command, ct);
            return Results.NoContent();
        });
    }
}
