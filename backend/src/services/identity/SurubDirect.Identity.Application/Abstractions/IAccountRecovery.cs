namespace SurubDirect.Identity.Application.Abstractions;
public interface IAccountRecovery
{
    Task RequestConfirmation(string email);
    Task<bool> ConfirmEmail(string userId, string token);
    Task RequestPasswordReset(string email);
    Task<bool> ResetPassword(string userId, string token, string password);
}
