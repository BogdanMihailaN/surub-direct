using SurubDirect.Purchasing.Api.Configuration;
using SurubDirect.Purchasing.Api.Endpoints;
using SurubDirect.Purchasing.Api.Middleware;
using SurubDirect.Purchasing.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddPurchasingService();
var app = builder.Build();
if (args.Contains("--initialize"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Use controlled migrations outside Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.MigratePurchasing();
    return;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapPurchasingEndpoints();
app.Run();
