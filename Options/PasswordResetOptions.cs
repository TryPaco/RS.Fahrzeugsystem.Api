namespace RS.Fahrzeugsystem.Api.Options;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public int TokenLifetimeMinutes { get; set; } = 60;
    public string ResetUrl { get; set; } = "http://localhost:5173/reset-password";
}
