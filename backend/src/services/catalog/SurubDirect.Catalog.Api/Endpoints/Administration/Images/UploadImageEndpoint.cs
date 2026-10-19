using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Infrastructure;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class UploadImageEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/products/{id:guid}/images",
        async (Guid id, string altText, Guid? variantId, HttpRequest request, UploadImageHandler handler, CancellationToken ct) =>
        {
            if (request.ContentLength > SkiaImageProcessor.MaximumBytes) throw new BusinessException("Imaginea poate avea maximum 8 MB.", 413);
            var imageId = await handler.Handle(id, request.Body, altText, variantId, ct);
            return Results.Created($"/api/v1/admin/products/{id}/images/{imageId}/full", new { id = imageId });
        });
}
