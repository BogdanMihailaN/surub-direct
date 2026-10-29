using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Identity.Api.Configuration;
using SurubDirect.Identity.Api.Endpoints;
using SurubDirect.Identity.Api.Middleware;
using SurubDirect.Identity.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace SurubDirect.Tests;
public sealed class CustomerIdentityFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17").Build();
    public WebApplication App { get; private set; } = null!;
    public const string Password = "Test-customer-12345!";
    public async Task InitializeAsync()
    {
        await database.StartAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Identity"] = database.GetConnectionString(),
            ["ServiceKeys:Identity"] = new string('x', 40),
            ["Logging:LogLevel:Default"] = "Warning"
        });
        builder.AddIdentityService();
        App = builder.Build();
        App.UseMiddleware<ApiExceptionMiddleware>();
        App.UseRateLimiter();
        App.UseAuthentication();
        App.UseAuthorization();
        App.UseMiddleware<AntiforgeryMiddleware>();
        App.MapIdentityEndpoints();
        App.MapGet("/test/admin", () => "admin").RequireAuthorization(p => p.RequireRole("Administrator"));
        await using var scope = App.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<StoreIdentityDbContext>().Database.MigrateAsync();
        await App.StartAsync();
    }
    public HttpClient Client() => new(new HttpClientHandler { CookieContainer = new(), AllowAutoRedirect = false })
        { BaseAddress = new Uri(App.Urls.Single()) };
    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
        await database.DisposeAsync();
    }
}
