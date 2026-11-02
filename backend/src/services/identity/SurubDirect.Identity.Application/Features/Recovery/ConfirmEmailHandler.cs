using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Application.Features.Recovery;
public sealed class ConfirmEmailHandler(IAccountRecovery accounts)
{
    public Task<bool> Handle(string? userId, string? token) => RecoveryInput.Token(userId, token) ? accounts.ConfirmEmail(userId!, token!) : Task.FromResult(false);
}
