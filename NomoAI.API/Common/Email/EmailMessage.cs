namespace NomoAI.API.Common.Email;

public sealed record EmailMessage(
    string Subject,
    string HtmlBody);
