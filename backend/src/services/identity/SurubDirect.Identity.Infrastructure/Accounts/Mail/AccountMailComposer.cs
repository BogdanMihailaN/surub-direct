using System.Net;

namespace SurubDirect.Identity.Infrastructure.Accounts.Mail;

public sealed class AccountMailComposer
{
    private readonly string origin;

    public AccountMailComposer(string storefrontOrigin, bool development)
    {
        if (!Uri.TryCreate(storefrontOrigin, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/"
            || (uri.Scheme != Uri.UriSchemeHttps
                && !(development && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new ArgumentException("AccountMail:StorefrontOrigin must be an HTTPS origin; HTTP loopback is allowed only in Development.", nameof(storefrontOrigin));
        origin = uri.GetLeftPart(UriPartial.Authority);
    }

    public AccountMailContent Compose(string kind, string userId, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var (path, subject, instruction, action) = kind switch
        {
            "confirm-email" => ("confirma-email", "Confirmă adresa de e-mail — Șurub Direct",
                "Pentru a confirma adresa de e-mail a contului tău, deschide linkul și apasă butonul de confirmare.", "Confirmă adresa"),
            "reset-password" => ("reseteaza-parola", "Resetează parola — Șurub Direct",
                "Pentru a alege o parolă nouă pentru contul tău, deschide linkul de mai jos.", "Resetează parola"),
            _ => throw new ArgumentException("Unsupported account mail kind.", nameof(kind))
        };
        var url = $"{origin}/cont/{path}#userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
        const string expiry = "Linkul este valabil cel mult două ore de la solicitare. Dacă a expirat, solicită un mesaj nou.";
        const string unsolicited = "Dacă nu ai solicitat această operațiune, ignoră mesajul. Nu trimite linkul altor persoane.";
        var text = $"Șurub Direct\n\n{instruction}\n\n{url}\n\n{expiry}\n\n{unsolicited}";
        var html = $"<!doctype html><html lang=\"ro\"><head><meta charset=\"utf-8\"><title>{WebUtility.HtmlEncode(subject)}</title></head><body><h1>Șurub Direct</h1><p>{WebUtility.HtmlEncode(instruction)}</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">{action}</a></p><p>{expiry}</p><p>{unsolicited}</p></body></html>";
        return new(subject, text, html, url);
    }
}
