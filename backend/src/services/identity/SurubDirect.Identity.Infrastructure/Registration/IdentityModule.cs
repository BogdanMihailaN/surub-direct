using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace SurubDirect.Identity.Infrastructure;
public static class IdentityModule
{
    public static IdentityBuilder AddStoreIdentity(this IServiceCollection s, string connection)
    {
        s.AddScoped<SurubDirect.Identity.Application.Abstractions.ICustomerAccounts, SurubDirect.Identity.Infrastructure.Accounts.CustomerAccounts>();
        s.AddScoped<SurubDirect.Identity.Application.Features.RegisterCustomer.RegisterCustomerHandler>();
        s.AddDbContext<StoreIdentityDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        return s.AddIdentity<IdentityUser, IdentityRole>(o =>
        {
            o.Password.RequiredLength = 12;
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.User.RequireUniqueEmail = true;
            o.User.AllowedUserNameCharacters = string.Empty;
        }).AddEntityFrameworkStores<StoreIdentityDbContext>().AddDefaultTokenProviders();
    }

    public static async Task InitializeIdentity(this IServiceProvider services, string email, string password)
    {
        await services.GetRequiredService<StoreIdentityDbContext>().Database.MigrateAsync();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Administrator"))
            await roles.CreateAsync(new("Administrator"));
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is not null && !await users.IsInRoleAsync(user, "Administrator"))
            throw new InvalidOperationException("Un cont existent fără rol Administrator nu poate fi promovat prin inițializare.");
        if (user == null)
        {
            user = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        var assigned = await users.AddToRoleAsync(user, "Administrator");
        if (!assigned.Succeeded && !await users.IsInRoleAsync(user, "Administrator"))
            throw new InvalidOperationException("Nu se poate atribui rolul administrator.");
    }
}
