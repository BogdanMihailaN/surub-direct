using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Identity.Application.Features.RegisterCustomer;
using SurubDirect.Identity.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class CustomerRegistrationTests(CustomerIdentityFixture fixture) : IClassFixture<CustomerIdentityFixture>
{
    private static async Task<HttpResponseMessage> Write(HttpClient client, string route, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
    [Fact]
    public async Task Registration_requires_csrf_even_when_called_directly()
    {
        using var client = fixture.Client();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "blocked@example.test", password = CustomerIdentityFixture.Password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync("blocked@example.test"));
    }
    [Fact]
    public async Task Customer_can_login_logout_but_cannot_access_administration_or_choose_roles()
    {
        using var client = fixture.Client();
        var email = $"customer-{Guid.NewGuid():N}@example.test";
        var registered = await Write(client, "/api/auth/register", new { email, password = CustomerIdentityFixture.Password, roles = new[] { "Administrator" }, emailConfirmed = true });
        Assert.Equal(HttpStatusCode.NoContent, registered.StatusCode);
        Assert.Equal("no-store", registered.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Write(client, "/api/auth/login", new { email, password = CustomerIdentityFixture.Password })).StatusCode);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.False(me.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/test/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Write(client, "/api/auth/register", new { email, password = CustomerIdentityFixture.Password })).StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = (await users.FindByEmailAsync(email))!;
        Assert.False(user.EmailConfirmed);
        Assert.Empty(await users.GetRolesAsync(user));
        Assert.Equal(HttpStatusCode.NoContent, (await Write(client, "/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact]
    public async Task Duplicate_case_insensitive_email_does_not_disclose_account_or_replace_password()
    {
        using var client = fixture.Client();
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        Assert.Equal(HttpStatusCode.NoContent, (await Write(client, "/api/auth/register", new { email, password = CustomerIdentityFixture.Password })).StatusCode);
        var duplicate = await Write(client, "/api/auth/register", new { email = email.ToUpperInvariant(), password = "Different-password-123!" });
        Assert.Equal(HttpStatusCode.NoContent, duplicate.StatusCode);
        Assert.Equal("", await duplicate.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await Write(client, "/api/auth/login", new { email, password = "Different-password-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Write(client, "/api/auth/login", new { email, password = CustomerIdentityFixture.Password })).StatusCode);
    }
    [Fact]
    public async Task Registration_is_unavailable_outside_development()
    {
        var environment = fixture.App.Services.GetRequiredService<IWebHostEnvironment>();
        var original = environment.EnvironmentName;
        try
        {
            environment.EnvironmentName = "Production";
            using var client = fixture.Client();
            Assert.Equal(HttpStatusCode.NotFound, (await Write(client, "/api/auth/register",
                new { email = "production@example.test", password = CustomerIdentityFixture.Password })).StatusCode);
        }
        finally { environment.EnvironmentName = original; }
    }
    [Fact]
    public async Task Initialization_cannot_promote_a_registered_customer()
    {
        var email = $"bootstrap-customer-{Guid.NewGuid():N}@example.test";
        await using var scope = fixture.App.Services.CreateAsyncScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<RegisterCustomerHandler>().Handle(email, CustomerIdentityFixture.Password));
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.InitializeIdentity(email, "Different-admin-password-123!"));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Empty(await users.GetRolesAsync((await users.FindByEmailAsync(email))!));
    }
    [Fact]
    public async Task Initialization_creates_administrator_and_can_be_repeated()
    {
        var email = $"bootstrap-admin-{Guid.NewGuid():N}@example.test";
        await using var scope = fixture.App.Services.CreateAsyncScope();
        await scope.ServiceProvider.InitializeIdentity(email, CustomerIdentityFixture.Password);
        await scope.ServiceProvider.InitializeIdentity(email, CustomerIdentityFixture.Password);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True(await users.IsInRoleAsync((await users.FindByEmailAsync(email))!, "Administrator"));
    }
    [Fact]
    public async Task Concurrent_registration_keeps_one_account()
    {
        var email = $"concurrent-{Guid.NewGuid():N}@example.test";
        await Task.WhenAll(Enumerable.Range(0, 3).Select(async _ =>
        {
            await using var scope = fixture.App.Services.CreateAsyncScope();
            Assert.True(await scope.ServiceProvider.GetRequiredService<RegisterCustomerHandler>().Handle(email, CustomerIdentityFixture.Password));
        }));
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Single(users.Users.Where(u => u.Email == email));
    }
}
