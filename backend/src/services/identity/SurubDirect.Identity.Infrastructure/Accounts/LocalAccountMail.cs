using System.Text.Json;
using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Infrastructure.Accounts;
public sealed class LocalAccountMail(string directory, bool enabled) : IAccountMail
{
    public async Task Save(string kind, string email, string userId, string token)
    {
        if (!enabled) throw new InvalidOperationException("Local account mail is restricted to Development.");
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory);
        else Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        var temporary = path + ".tmp";
        try
        {
            await using (var file = new FileStream(temporary, options))
                await JsonSerializer.SerializeAsync(file, new { kind, email, userId, token, createdAt = DateTimeOffset.UtcNow, expiresAt = DateTimeOffset.UtcNow.AddHours(2) });
            File.Move(temporary, path);
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }
    }
}
