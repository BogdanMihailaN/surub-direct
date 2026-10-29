using SurubDirect.Identity.Application.Abstractions;
namespace SurubDirect.Tests;
public sealed class CapturingCustomerAccounts : ICustomerAccounts
{
    public string? Email { get; private set; }
    public string? Password { get; private set; }
    public Task Create(string email, string password)
    {
        Email = email;
        Password = password;
        return Task.CompletedTask;
    }
}
