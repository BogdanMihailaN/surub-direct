using SurubDirect.Identity.Api.Configuration;
using SurubDirect.Identity.Api.Endpoints;
using SurubDirect.Identity.Api.Middleware;
using SurubDirect.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddIdentityService();
var app = builder.Build();
if (args.Contains("--initialize"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Use controlled migrations outside Development.");
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.InitializeIdentity(app.Configuration["Admin:Email"] ?? throw new InvalidOperationException("Configure Admin__Email."), app.Configuration["Admin:Password"] ?? throw new InvalidOperationException("Configure Admin__Password."));
    return;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryMiddleware>();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.MapIdentityEndpoints();
app.Run();
