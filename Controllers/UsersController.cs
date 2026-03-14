using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission("users.view")]
    public async Task<ActionResult> GetAll()
    {
        var users = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .Select(x => new
            {
                x.Id,
                x.Username,
                x.DisplayName,
                x.Email,
                x.IsActive,
                Roles = x.UserRoles.Select(ur => ur.Role.Name)
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    [HasPermission("users.manage")]
    public async Task<ActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (await dbContext.Users.AnyAsync(x => x.Username == request.Username))
            return Conflict("Benutzername existiert bereits.");

        if (await dbContext.Users.AnyAsync(x => x.Email == request.Email))
            return Conflict("E-Mail existiert bereits.");

        var roles = await dbContext.Roles.Where(x => request.Roles.Contains(x.Name)).ToListAsync();
        var user = new User
        {
            Username = request.Username,
            DisplayName = request.DisplayName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = request.IsActive
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        foreach (var role in roles)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, new { user.Id });
    }

    [HttpPut("{id:guid}")]
    [HasPermission("users.manage")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await dbContext.Users.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.UserRoles.RemoveRange(user.UserRoles);
        var roles = await dbContext.Roles.Where(x => request.Roles.Contains(x.Name)).ToListAsync();
        foreach (var role in roles)
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }

        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
