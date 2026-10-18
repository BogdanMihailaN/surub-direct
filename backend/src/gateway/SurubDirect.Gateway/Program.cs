using SurubDirect.Gateway.Configuration;
using SurubDirect.Gateway.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddGatewayRateLimiting();
var app = builder.Build();
app.UseRateLimiter();
LivenessEndpoint.Map(app);
app.MapReverseProxy();
app.Run();
