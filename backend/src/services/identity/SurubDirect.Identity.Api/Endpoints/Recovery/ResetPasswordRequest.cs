namespace SurubDirect.Identity.Api.Endpoints;
public sealed record ResetPasswordRequest(string? UserId, string? Token, string? Password);
