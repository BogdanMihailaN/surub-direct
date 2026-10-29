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
app.UseAuthentication();
app.UseAuthorization();
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
PrepareCartEndpoint.Map(app);
GetPreparationEndpoint.Map(app);
CancelPreparationEndpoint.Map(app);
ConfirmDemoOrderEndpoint.Map(app);
CompleteDemoOrderEndpoint.Map(app);
GetDemoOrdersEndpoint.Map(app);
GetAccountOrdersEndpoint.Map(app);
LinkAccountOrderEndpoint.Map(app);
ListAdminOrdersEndpoint.Map(app);
GetAdminOrderEndpoint.Map(app);
CancelDemoOrderEndpoint.Map(app);
GetBrokerStatusEndpoint.Map(app);
GetMessagingStatusEndpoint.Map(app);
ReplayMessageEndpoint.Map(app);
app.Run();
