using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NomoAI.API.Common.Abstractions.Email;
namespace NomoAI.API.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _emailOptions;
    private readonly IHostEnvironment _environment;

    public SmtpEmailSender(
        IOptions<EmailOptions> emailOptions,
        IHostEnvironment environment)
    {
        _emailOptions = emailOptions.Value;
        _environment = environment;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _emailOptions.FromName,
                _emailOptions.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(toEmail));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();

       
        smtpClient.CheckCertificateRevocation =
            !_environment.IsDevelopment();

        try
        {
            var secureSocketOptions =
                _emailOptions.UseSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

            await smtpClient.ConnectAsync(
                _emailOptions.Host,
                _emailOptions.Port,
                secureSocketOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                    _emailOptions.Username))
            {
                await smtpClient.AuthenticateAsync(
                    _emailOptions.Username,
                    _emailOptions.Password,
                    cancellationToken);
            }

            await smtpClient.SendAsync(
                message,
                cancellationToken);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    quit: true,
                    CancellationToken.None);
            }
        }
    }
}