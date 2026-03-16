namespace RS.Fahrzeugsystem.Api.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody = null);
}
