namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record LoginRequest(string Username, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string Username,
    string DisplayName,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions);
