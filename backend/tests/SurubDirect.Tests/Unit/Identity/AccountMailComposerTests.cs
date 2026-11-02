using SurubDirect.Identity.Infrastructure.Accounts.Mail;
using Xunit;

namespace SurubDirect.Tests;

public sealed class AccountMailComposerTests
{
    [Theory]
    [InlineData("confirm-email", "/cont/confirma-email", "Confirmă")]
    [InlineData("reset-password", "/cont/reseteaza-parola", "Resetează")]
    public void Links_preserve_opaque_values_in_fragment_and_bodies_match_the_operation(string kind, string path, string subject)
    {
        const string token = "a+b/c==&token=other#<>\"é";
        const string user = "user+&=#";
        var mail = new AccountMailComposer("https://shop.example.test/", false).Compose(kind, user, token);
        var uri = new Uri(mail.ActionUrl);
        Assert.Equal("https://shop.example.test", uri.GetLeftPart(UriPartial.Authority));
        Assert.Equal(path, uri.AbsolutePath);
        Assert.Empty(uri.Query);
        var values = uri.Fragment[1..].Split('&').Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
        Assert.Equal(user, values["userId"]);
        Assert.Equal(token, values["token"]);
        Assert.Equal(2, values.Count);
        Assert.Contains(subject, mail.Subject);
        Assert.Contains(mail.ActionUrl, mail.TextBody);
        Assert.Contains("&amp;token=", mail.HtmlBody);
        Assert.DoesNotContain(token, mail.HtmlBody);
        Assert.Contains("două ore", mail.TextBody);
    }

    [Theory]
    [InlineData("http://shop.example.test", true)]
    [InlineData("http://localhost:4200", false)]
    [InlineData("https://user:password@shop.example.test", false)]
    [InlineData("https://shop.example.test/path", false)]
    [InlineData("https://shop.example.test/?next=evil", false)]
    [InlineData("https://shop.example.test/#fragment", false)]
    [InlineData("javascript:alert(1)", true)]
    [InlineData("/relative", true)]
    public void Invalid_origins_are_rejected(string origin, bool development) =>
        Assert.Throws<ArgumentException>(() => new AccountMailComposer(origin, development));

    [Fact]
    public void Local_development_links_work_but_unknown_operations_are_rejected()
    {
        var composer = new AccountMailComposer("http://localhost:4200", true);
        Assert.StartsWith("http://localhost:4200/cont/", composer.Compose("confirm-email", "user", "token").ActionUrl);
        Assert.Throws<ArgumentException>(() => composer.Compose("unknown", "user", "token"));
    }
}
