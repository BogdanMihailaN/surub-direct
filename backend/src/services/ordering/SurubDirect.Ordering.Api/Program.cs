using SurubDirect.Ordering.Infrastructure;
using SurubDirect.Ordering.Api.Configuration;
using SurubDirect.Ordering.Api.Endpoints;
using SurubDirect.Ordering.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.AddOrdering();
var app = builder.Build();
if (args.Contains("--initialize"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.MigrateOrdering();
    return;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseRateLimiter();
CapabilitiesEndpoint.Map(app);
QuoteCartEndpoint.Map(app);
LivenessEndpoint.Map(app);
ReadinessEndpoint.Map(app);
GetCartTokenEndpoint.Map(app);
GetCartEndpoint.Map(app);
CreateCartSessionEndpoint.Map(app);
ReplaceCartEndpoint.Map(app);
SaveCartCustomerEndpoint.Map(app);
PlanReservationEndpoint.Map(app, app.Configuration);
CancelReservationPlanEndpoint.Map(app, app.Configuration);
GetReservationPlanEndpoint.Map(app, app.Configuration);
app.Run();
