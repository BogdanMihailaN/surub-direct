using System.ComponentModel.DataAnnotations;
namespace SurubDirect.Identity.Application.Features.Recovery;
internal static class RecoveryInput
{
    public static bool Email(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 254 && new EmailAddressAttribute().IsValid(value);
    public static bool Token(string? userId, string? token) => !string.IsNullOrWhiteSpace(userId) && userId.Length <= 450 && !string.IsNullOrWhiteSpace(token) && token.Length <= 4096;
    public static bool Password(string? value) => value is { Length: >= 12 and <= 128 } && value.Any(c => c is >= 'A' and <= 'Z') && value.Any(c => c is >= 'a' and <= 'z') && value.Any(c => c is >= '0' and <= '9') && !value.All(char.IsAsciiLetterOrDigit);
}
