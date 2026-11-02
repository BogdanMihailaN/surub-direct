using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SurubDirect.Identity.Application.Abstractions;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AccountRecoveryTests(CustomerIdentityFixture fixture) : IClassFixture<CustomerIdentityFixture>
{
    private static async Task<HttpResponseMessage> Write(HttpClient client, string path, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/" + path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
    private async Task<IdentityUser> Customer()
    {
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"recovery-{Guid.NewGuid():N}@example.test";
        var user = new IdentityUser { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, CustomerIdentityFixture.Password)).Succeeded);
        return user;
    }
    private JsonElement Message(string email, string kind) => Directory.GetFiles(fixture.MailDirectory, "*.json")
        .Select(path => JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path)))
        .Single(x => x.GetProperty("email").GetString() == email && x.GetProperty("kind").GetString() == kind);
    [Fact]
    public async Task Confirmation_requests_do_not_disclose_accounts_and_tokens_are_bound_to_user_and_purpose()
    {
        var user = await Customer();
        using var client = fixture.Client();
        foreach (var email in new[] { user.Email!, "unknown@example.test" })
        {
            var response = await Write(client, "request-email-confirmation", new { email });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        }
        var mail = Message(user.Email!, "confirm-email");
        var token = mail.GetProperty("token").GetString();
        Assert.Equal(HttpStatusCode.BadRequest, (await Write(client, "confirm-email", new { userId = "other", token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Write(client, "reset-password", new { userId = user.Id, token, password = "Replacement-pass-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Write(client, "confirm-email", new { userId = user.Id, token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Write(client, "confirm-email", new { userId = user.Id, token })).StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByIdAsync(user.Id))!.EmailConfirmed);
    }
    [Fact]
    public async Task Reset_changes_password_once_and_revokes_existing_sessions_including_internal_validation()
    {
        var user = await Customer();
        using var session = fixture.Client();
        using var anonymous = fixture.Client();
        Assert.Equal(HttpStatusCode.OK, (await Write(session, "login", new { email = user.Email, password = CustomerIdentityFixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Write(anonymous, "request-password-reset", new { email = user.Email })).StatusCode);
        var token = Message(user.Email!, "reset-password").GetProperty("token").GetString();
        var password = "Replacement-pass-123!";
        Assert.Equal(HttpStatusCode.BadRequest, (await Write(anonymous, "reset-password", new { userId = user.Id, token, password = "weak" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Write(anonymous, "reset-password", new { userId = user.Id, token, password })).StatusCode);
        using var validation = new HttpRequestMessage(HttpMethod.Post, "/internal/session");
        validation.Headers.Add("X-Service-Key", new string('x', 40));
        validation.Headers.Add("X-Original-Method", "GET");
        Assert.Equal(HttpStatusCode.Unauthorized, (await session.SendAsync(validation)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Write(anonymous, "reset-password", new { userId = user.Id, token, password })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Write(anonymous, "login", new { email = user.Email, password = CustomerIdentityFixture.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Write(anonymous, "login", new { email = user.Email, password })).StatusCode);
    }
    [Fact]
    public async Task Expired_tokens_and_administrator_accounts_are_not_recovered()
    {
        var user = await Customer();
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var recovery = scope.ServiceProvider.GetRequiredService<IAccountRecovery>();
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var confirmation = await users.GenerateEmailConfirmationTokenAsync(user);
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;
        var lifespan = options.TokenLifespan;
        try
        {
            options.TokenLifespan = TimeSpan.FromSeconds(-1);
            Assert.False(await recovery.ResetPassword(user.Id, token, "Replacement-pass-123!"));
            Assert.False(await recovery.ConfirmEmail(user.Id, confirmation));
        }
        finally { options.TokenLifespan = lifespan; }
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole("Administrator"));
        user = (await users.FindByIdAsync(user.Id))!;
        Assert.True((await users.AddToRoleAsync(user, "Administrator")).Succeeded);
        await recovery.RequestPasswordReset(user.Email!);
        await recovery.RequestConfirmation(user.Email!);
        Assert.False(await recovery.ResetPassword(user.Id, token, "Replacement-pass-123!"));
        Assert.False(await recovery.ConfirmEmail(user.Id, await users.GenerateEmailConfirmationTokenAsync(user)));
        Assert.DoesNotContain(Directory.Exists(fixture.MailDirectory) ? Directory.GetFiles(fixture.MailDirectory) : [], path => File.ReadAllText(path).Contains(user.Email!));
    }
    [Fact]
    public async Task Concurrent_resets_with_the_same_token_can_only_commit_once()
    {
        var user = await Customer();
        await using var issuing = fixture.App.Services.CreateAsyncScope();
        var token = await issuing.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().GeneratePasswordResetTokenAsync(user);
        async Task<bool> Reset()
        {
            await using var scope = fixture.App.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IAccountRecovery>().ResetPassword(user.Id, token, "Replacement-pass-123!");
        }
        var results = await Task.WhenAll(Reset(), Reset());
        Assert.Single(results, x => x);
    }
    [Fact]
    public async Task Recovery_requires_csrf_and_is_disabled_outside_development()
    {
        using var client = fixture.Client();
        var paths = new[] { "request-email-confirmation", "request-password-reset", "confirm-email", "reset-password" };
        foreach (var path in paths)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/" + path, new { })).StatusCode);
        var environment = fixture.App.Services.GetRequiredService<IWebHostEnvironment>();
        var previous = environment.EnvironmentName;
        try
        {
            environment.EnvironmentName = "Production";
            foreach (var path in paths)
                Assert.Equal(HttpStatusCode.NotFound, (await Write(client, path, new { email = "test@example.test", userId = "test", token = "invalid", password = CustomerIdentityFixture.Password })).StatusCode);
        }
        finally { environment.EnvironmentName = previous; }
    }
    [Fact]
    public async Task Local_messages_use_private_permissions_and_unknown_reset_requests_remain_generic()
    {
        var user = await Customer();
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.NoContent, (await Write(client, "request-password-reset", new { email = "absent@example.test" })).StatusCode);
        await using var scope = fixture.App.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAccountRecovery>().RequestPasswordReset(user.Email!);
        var path = Directory.GetFiles(fixture.MailDirectory).Single(path => File.ReadAllText(path).Contains(user.Email!));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
    }
}
