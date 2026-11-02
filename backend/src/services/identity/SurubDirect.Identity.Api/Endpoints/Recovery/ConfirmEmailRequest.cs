namespace SurubDirect.Identity.Api.Endpoints;
public sealed record ConfirmEmailRequest(string? UserId, string? Token);
