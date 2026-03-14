using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Services;

public interface ITokenService
{
    string CreateToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, out DateTime expiresAtUtc);
}
