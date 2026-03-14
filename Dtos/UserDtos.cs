namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Email,
    string Password,
    bool IsActive,
    List<string> Roles);

public sealed record UpdateUserRequest(
    string DisplayName,
    string Email,
    bool IsActive,
    List<string> Roles);
