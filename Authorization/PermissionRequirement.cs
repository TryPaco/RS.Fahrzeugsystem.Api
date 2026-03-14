using Microsoft.AspNetCore.Authorization;

namespace RS.Fahrzeugsystem.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
