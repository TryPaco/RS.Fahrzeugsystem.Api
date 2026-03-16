using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Options;
using System.Net;
using System.Net.Mail;

namespace RS.Fahrzeugsystem.Api.Services;

public sealed class SmtpEmailService(AppDbContext dbContext, IOptions<SmtpOptions> options) : IEmailService
{
    private readonly SmtpOptions _smtpOptions = options.Value;

    public async Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody = null)
    {
        var persistedSettings = await dbContext.SmtpConfigurations
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var host = persistedSettings?.Host ?? _smtpOptions.Host;
        var port = persistedSettings?.Port ?? _smtpOptions.Port;
        var username = persistedSettings?.Username ?? _smtpOptions.Username;
        var password = persistedSettings?.Password ?? _smtpOptions.Password;
        var fromEmail = persistedSettings?.FromEmail ?? _smtpOptions.FromEmail;
        var fromName = persistedSettings?.FromName ?? _smtpOptions.FromName;
        var enableSsl = persistedSettings?.EnableSsl ?? _smtpOptions.EnableSsl;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP-Konfiguration ist unvollständig.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        await client.SendMailAsync(message);
    }
}
