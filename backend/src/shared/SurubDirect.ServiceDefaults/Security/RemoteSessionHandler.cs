using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.ServiceDefaults.Security;
public sealed class RemoteSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IHttpClientFactory clients) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Path.StartsWithSegments("/api/v1/admin") || !Request.Headers.ContainsKey("Cookie"))
            return AuthenticateResult.NoResult();
        using var message = new HttpRequestMessage(HttpMethod.Post, "/internal/session");
        message.Headers.TryAddWithoutValidation("Cookie", Request.Headers.Cookie.ToString());
        message.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", Request.Headers["X-CSRF-TOKEN"].ToString());
        message.Headers.Add("X-Original-Method", Request.Method);
        try
        {
            using var response = await clients.CreateClient("identity").SendAsync(message, Context.RequestAborted);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return AuthenticateResult.Fail("Session expired.");
            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new BusinessException("Sesiunea formularului a expirat. Reîncarcă pagina.", 400);
            if (!response.IsSuccessStatusCode)
                throw new BusinessException("Autentificarea nu este disponibilă momentan.", 503);
            var session = await response.Content.ReadFromJsonAsync<SessionPrincipal>(Context.RequestAborted) ?? throw new BusinessException("Autentificarea nu este disponibilă momentan.", 503);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.Id),
                new(ClaimTypes.Name, session.Email)
            };
            claims.AddRange(session.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
        }
        catch (HttpRequestException)
        {
            throw new BusinessException("Serviciul de autentificare nu răspunde.", 503);
        }
        catch (OperationCanceledException)when (!Context.RequestAborted.IsCancellationRequested)
        {
            throw new BusinessException("Serviciul de autentificare nu răspunde.", 503);
        }
    }
}
