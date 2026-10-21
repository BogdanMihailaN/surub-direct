using System.Security.Cryptography;
using System.Text;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Api.Security;
public sealed class CartOwnerCookie(IWebHostEnvironment environment)
{
    public const string Name = "SurubDirect.Cart";
    public string? Read(HttpContext context)
    {
        var token = context.Request.Cookies[Name];
        return token is { Length: 64 } && token.All(Uri.IsHexDigit) ? Hash(token) : null;
    }

    public string Require(HttpContext context) => Read(context) ?? throw new BusinessException("Coșul nu mai este accesibil. Reîncarcă pagina.", 401);
    public string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public void Write(HttpContext context, string token, DateTimeOffset expires) => context.Response.Cookies.Append(Name, token, new CookieOptions { HttpOnly = true, Secure = !environment.IsDevelopment() || context.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/api/v1/ordering", Expires = expires, IsEssential = true });
}
