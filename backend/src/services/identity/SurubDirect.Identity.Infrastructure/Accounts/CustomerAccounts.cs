using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SurubDirect.Identity.Application.Abstractions;

namespace SurubDirect.Identity.Infrastructure.Accounts;
public sealed class CustomerAccounts(UserManager<IdentityUser> users) : ICustomerAccounts
{
    public async Task Create(string email, string password)
    {
        if (await users.FindByEmailAsync(email) is not null)
            return;
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = false };
        try
        {
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded && !result.Errors.All(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
                throw new InvalidOperationException("Customer account could not be created.");
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UserNameIndex" })
        {
            // Concurrent registration for the same normalized email has the same public outcome.
        }
    }
}
