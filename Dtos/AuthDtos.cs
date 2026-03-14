namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    string Username,
    string DisplayName,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions);
