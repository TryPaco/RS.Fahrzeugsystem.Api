using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Services;
using System.Security.Claims;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext dbContext, ITokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.RolePermissions)
                        .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Username == request.Username);

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Ungültige Anmeldedaten.");
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return Unauthorized("Ungültige Anmeldedaten.");
        }

        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().ToArray();
        var permissions = user.UserRoles
            .SelectMany(x => x.Role.RolePermissions)
            .Select(x => x.Permission.Key)
            .Distinct()
            .ToArray();

        var token = tokenService.CreateToken(user, roles, permissions, out var expiresAtUtc);
        return Ok(new LoginResponse(token, expiresAtUtc, user.Username, user.DisplayName, roles, permissions));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            username = User.Identity?.Name,
            displayName = User.FindFirstValue("display_name"),
            roles = User.FindAll(ClaimTypes.Role).Select(x => x.Value),
            permissions = User.FindAll("permission").Select(x => x.Value)
        });
    }
}
