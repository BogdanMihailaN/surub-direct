using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace SurubDirect.Tests;
public sealed class AccountRecoveryRateLimitTests(CustomerIdentityFixture fixture) : IClassFixture<CustomerIdentityFixture>
{
    [Fact]
    public async Task Repeated_recovery_requests_are_limited_without_queuing()
    {
        using var client = fixture.Client();
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        for (var attempt = 0; attempt < 21; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/request-password-reset")
            {
                Content = JsonContent.Create(new { email = "unknown@example.test" })
            };
            request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
            var response = await client.SendAsync(request);
            Assert.Equal(attempt < 20 ? HttpStatusCode.NoContent : HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
