using SurubDirect.Store.Application.Imports;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class CommitImportEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapPost("/products/import/commit",
        async (ImportRequest request, CommitImportHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(request.Csv, ct)))
        .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(8 * ImportCsvFormat.MaximumBytes));
}
