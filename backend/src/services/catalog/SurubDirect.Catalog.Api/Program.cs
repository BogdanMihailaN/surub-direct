using SurubDirect.Catalog.Api.Bootstrap;
using SurubDirect.Catalog.Api.Configuration;
using SurubDirect.Catalog.Api.Endpoints;
using SurubDirect.Catalog.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.AddCatalogService();
var app = builder.Build();
if (args.Contains("--initialize"))
{
    await app.InitializeCatalog(args.Contains("--seed-demo"));
    return;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapCatalogEndpoints();
app.Run();
