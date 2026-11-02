using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Identity.Application.Features.Recovery;
public sealed class RequestEmailConfirmationHandler(IAccountRecovery accounts)
{
    public async Task<bool> Handle(string? email)
    {
        email = email?.Trim();
        if (!RecoveryInput.Email(email)) return false;
        await accounts.RequestConfirmation(email!);
        return true;
    }
}
