namespace SurubDirect.Identity.Application.Abstractions;
public interface ICustomerAccounts
{
    Task Create(string email, string password);
}
