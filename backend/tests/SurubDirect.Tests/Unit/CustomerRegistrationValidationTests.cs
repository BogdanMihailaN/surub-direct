using SurubDirect.Identity.Application.Abstractions;
using SurubDirect.Identity.Application.Features.RegisterCustomer;
using Xunit;

namespace SurubDirect.Tests;
public sealed class CustomerRegistrationValidationTests
{
    [Theory]
    [InlineData(null, "Test-customer-12345!")]
    [InlineData("person@example.test", "Éééééééééééé1!")]
    [InlineData("bad-email", "Test-customer-12345!")]
    [InlineData("person@example.test", null)]
    [InlineData("person@example.test", "short")]
    [InlineData("person@example.test", "long-without-uppercase-1!")]
    [InlineData("person@example.test", "PASSWORD-12345!")]
    [InlineData("person@example.test", "PasswordWithoutDigits!")]
    [InlineData("person@example.test", "PasswordWithoutSymbol123")]
    public async Task Invalid_input_does_not_reach_persistence(string? email, string? password)
    {
        var accounts = new CapturingCustomerAccounts();
        Assert.False(await new RegisterCustomerHandler(accounts).Handle(email, password));
        Assert.Null(accounts.Email);
    }
    [Fact]
    public async Task Limits_input_lengths_and_trims_email_only()
    {
        var accounts = new CapturingCustomerAccounts();
        var handler = new RegisterCustomerHandler(accounts);
        Assert.False(await handler.Handle(new string('a', 255) + "@example.test", "Test-customer-12345!"));
        Assert.False(await handler.Handle("person@example.test", "Ab1!" + new string('x', 125)));
        Assert.True(await handler.Handle(" person@example.test ", " Test-customer-12345! "));
        Assert.Equal("person@example.test", accounts.Email);
        Assert.Equal(" Test-customer-12345! ", accounts.Password);
    }
}
