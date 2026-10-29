using System.ComponentModel.DataAnnotations;
using SurubDirect.Identity.Application.Abstractions;

namespace SurubDirect.Identity.Application.Features.RegisterCustomer;
public sealed class RegisterCustomerHandler(ICustomerAccounts accounts)
{
    public async Task<bool> Handle(string? email, string? password)
    {
        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email)
            || string.IsNullOrEmpty(password) || password.Length is < 12 or > 128
            || !password.Any(c => c is >= 'A' and <= 'Z') || !password.Any(c => c is >= 'a' and <= 'z')
            || !password.Any(c => c is >= '0' and <= '9') || password.All(char.IsAsciiLetterOrDigit))
            return false;
        await accounts.Create(email, password);
        return true;
    }
}
