namespace SurubDirect.Identity.Application.Abstractions;
public interface IAccountMail
{
    Task Save(string kind, string email, string userId, string token);
}
