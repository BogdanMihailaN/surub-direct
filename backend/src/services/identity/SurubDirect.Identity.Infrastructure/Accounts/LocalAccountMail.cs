using System.Text.Json;
using SurubDirect.Identity.Infrastructure.Accounts.Mail;
using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Infrastructure.Accounts;
public sealed class LocalAccountMail(string directory, bool enabled, AccountMailComposer? composer) : IAccountMail
{
    public async Task Save(string kind, string email, string userId, string token)
    {
        if (!enabled || composer is null) throw new InvalidOperationException("Local account mail is restricted to Development.");
        var content = composer.Compose(kind, userId, token);
        var createdAt = DateTimeOffset.UtcNow;
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory);
        else Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        var temporary = path + ".tmp";
        try
        {
            await using (var file = new FileStream(temporary, options))
                await JsonSerializer.SerializeAsync(file, new { kind, email, userId, token, createdAt, expiresAt = createdAt.AddHours(2), subject = content.Subject, textBody = content.TextBody, htmlBody = content.HtmlBody, actionUrl = content.ActionUrl });
            File.Move(temporary, path);
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }
    }
}
