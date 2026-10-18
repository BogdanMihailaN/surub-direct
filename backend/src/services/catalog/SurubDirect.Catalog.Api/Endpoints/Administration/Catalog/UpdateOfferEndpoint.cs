using SurubDirect.Catalog.Application;

namespace SurubDirect.Catalog.Api.Endpoints;
public static class UpdateOfferEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPut("/products/{id:guid}/variants/{variantId:guid}/offers/{offerId:guid}",
            async (Guid id, Guid variantId, Guid offerId, UpdateOffer command, UpdateOfferHandler handler, CancellationToken ct) =>
                Results.Ok(new { packSizeChanged = await handler.Handle(id, variantId, offerId, command, ct) }));
    }
}
