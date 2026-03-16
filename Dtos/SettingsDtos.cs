namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record SmtpSettingsResponse(
    string Host,
    int Port,
    string Username,
    string FromEmail,
    string FromName,
    bool EnableSsl,
    bool HasPassword);

public sealed record UpdateSmtpSettingsRequest(
    string Host,
    int Port,
    string Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool EnableSsl);
