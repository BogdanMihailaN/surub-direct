using Npgsql;
using SurubDirect.Ordering.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(NpgsqlDataSource.Create(builder.Configuration.GetConnectionString("Ordering") ?? throw new InvalidOperationException("Configure ConnectionStrings__Ordering.")));
var app = builder.Build();
CapabilitiesEndpoint.Map(app);
LivenessEndpoint.Map(app);
ReadinessEndpoint.Map(app);
app.Run();
