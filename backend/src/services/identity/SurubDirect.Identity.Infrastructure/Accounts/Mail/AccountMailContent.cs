namespace SurubDirect.Identity.Infrastructure.Accounts.Mail;

public sealed record AccountMailContent(string Subject, string TextBody, string HtmlBody, string ActionUrl);
