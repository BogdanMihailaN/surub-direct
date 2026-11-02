using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Application.Features.Recovery;
public sealed class ResetPasswordHandler(IAccountRecovery accounts)
{
    public Task<bool> Handle(string? userId, string? token, string? password) => RecoveryInput.Token(userId, token) && RecoveryInput.Password(password) ? accounts.ResetPassword(userId!, token!, password!) : Task.FromResult(false);
}
