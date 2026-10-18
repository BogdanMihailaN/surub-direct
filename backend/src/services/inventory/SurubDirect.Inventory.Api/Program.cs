using SurubDirect.Inventory.Api.Configuration;
using SurubDirect.Inventory.Api.Endpoints;
using SurubDirect.Inventory.Api.Middleware;
using SurubDirect.Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddInventoryService();
var app = builder.Build();
if (args.Contains("--initialize"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Use controlled migrations outside Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.MigrateInventory();
    return;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapInventoryEndpoints();
app.Run();
