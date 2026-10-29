using System.Security.Claims;
using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Api.Security;
internal static class AccountCartIdentity
{
    public static string Require(HttpContext context, string expectedAccountId)
    {
        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(accountId)) throw new BusinessException("Autentifică-te pentru a accesa coșul contului.", 401);
        if (accountId != expectedAccountId) throw new BusinessException("Contul autentificat s-a schimbat. Reîncarcă pagina contului.", 409);
        return accountId;
    }
}
