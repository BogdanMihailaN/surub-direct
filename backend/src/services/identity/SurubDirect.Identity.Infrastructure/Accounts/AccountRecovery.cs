using Microsoft.AspNetCore.Identity;
using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Infrastructure.Accounts;
public sealed class AccountRecovery(UserManager<IdentityUser> users, IAccountMail mail) : IAccountRecovery
{
    public async Task RequestConfirmation(string email)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed || await users.IsInRoleAsync(user, "Administrator")) return;
        await mail.Save("confirm-email", user.Email!, user.Id, await users.GenerateEmailConfirmationTokenAsync(user));
    }
    public async Task RequestPasswordReset(string email)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null || await users.IsInRoleAsync(user, "Administrator")) return;
        await mail.Save("reset-password", user.Email!, user.Id, await users.GeneratePasswordResetTokenAsync(user));
    }
    public async Task<bool> ConfirmEmail(string userId, string token)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || user.EmailConfirmed || await users.IsInRoleAsync(user, "Administrator")) return false;
        return (await users.ConfirmEmailAsync(user, token)).Succeeded;
    }
    public async Task<bool> ResetPassword(string userId, string token, string password)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null || await users.IsInRoleAsync(user, "Administrator")) return false;
        return (await users.ResetPasswordAsync(user, token, password)).Succeeded;
    }
}
