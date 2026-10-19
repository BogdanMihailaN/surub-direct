using System.Text;
using SurubDirect.Store.Application.Imports;
namespace SurubDirect.Catalog.Api.Endpoints;
public static class GetImportTemplateEndpoint
{
    public static void Map(IEndpointRouteBuilder routes) => routes.MapGet("/products/import/template",
        () => Results.File(Encoding.UTF8.GetBytes("\uFEFF" + ImportCsvFormat.Template), "text/csv; charset=utf-8", "model-import-produse.csv"));
}
