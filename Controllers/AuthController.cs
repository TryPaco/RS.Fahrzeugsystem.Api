using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;
using RS.Fahrzeugsystem.Api.Options;
using RS.Fahrzeugsystem.Api.Services;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext dbContext,
    ITokenService tokenService,
    IEmailService emailService,
    IOptions<PasswordResetOptions> passwordResetOptions) : ControllerBase
{
    private readonly PasswordResetOptions _passwordResetOptions = passwordResetOptions.Value;

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
            return Unauthorized("Ungueltige Anmeldedaten.");
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            return Unauthorized("Ungueltige Anmeldedaten.");
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

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("E-Mail ist erforderlich.");
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == request.Email && x.IsActive);

        if (user is not null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            var tokenHash = HashToken(token);
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_passwordResetOptions.TokenLifetimeMinutes);

            var existingTokens = await dbContext.PasswordResetTokens
                .Where(x => x.UserId == user.Id && x.UsedAtUtc == null)
                .ToListAsync();

            dbContext.PasswordResetTokens.RemoveRange(existingTokens);

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAtUtc = expiresAtUtc
            };

            dbContext.PasswordResetTokens.Add(resetToken);
            await dbContext.SaveChangesAsync();

            var resetLink = $"{_passwordResetOptions.ResetUrl}?token={Uri.EscapeDataString(token)}";
            var textBody =
                $"Hallo {user.DisplayName},\n\n" +
                $"du kannst dein Passwort ueber folgenden Link zuruecksetzen:\n{resetLink}\n\n" +
                $"Der Link ist bis {expiresAtUtc:dd.MM.yyyy HH:mm} UTC gueltig.\n\n" +
                "Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.";

            var htmlBody =
                $"<p>Hallo {System.Net.WebUtility.HtmlEncode(user.DisplayName)},</p>" +
                $"<p>du kannst dein Passwort ueber folgenden Link zuruecksetzen:</p>" +
                $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(resetLink)}\">Passwort zuruecksetzen</a></p>" +
                $"<p>Der Link ist bis {expiresAtUtc:dd.MM.yyyy HH:mm} UTC gueltig.</p>" +
                "<p>Falls du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.</p>";

            try
            {
                await emailService.SendAsync(user.Email, "Passwort zuruecksetzen", textBody, htmlBody);
            }
            catch (InvalidOperationException)
            {
                dbContext.PasswordResetTokens.Remove(resetToken);
                await dbContext.SaveChangesAsync();

                return Problem(
                    title: "E-Mail-Versand nicht verfuegbar",
                    detail: "Die SMTP-Konfiguration ist unvollstaendig. Bitte die E-Mail-Einstellungen pruefen.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (SmtpException)
            {
                dbContext.PasswordResetTokens.Remove(resetToken);
                await dbContext.SaveChangesAsync();

                return Problem(
                    title: "E-Mail-Versand fehlgeschlagen",
                    detail: "Der SMTP-Server lehnt die konfigurierte Absenderadresse oder Anmeldung ab. Bitte die SMTP-Einstellungen pruefen.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }

        return Ok(new
        {
            message = "Wenn ein passender Benutzer existiert, wurde eine E-Mail zum Zuruecksetzen versendet."
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest("Token ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Neues Passwort ist erforderlich.");
        }

        var tokenHash = HashToken(request.Token);

        var resetToken = await dbContext.PasswordResetTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash);

        if (resetToken is null ||
            resetToken.UsedAtUtc is not null ||
            resetToken.ExpiresAtUtc < DateTime.UtcNow ||
            !resetToken.User.IsActive)
        {
            return BadRequest("Reset-Token ist ungueltig oder abgelaufen.");
        }

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAtUtc = DateTime.UtcNow;
        resetToken.UsedAtUtc = DateTime.UtcNow;

        var otherTokens = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == resetToken.UserId && x.Id != resetToken.Id && x.UsedAtUtc == null)
            .ToListAsync();

        foreach (var token in otherTokens)
        {
            token.UsedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return BadRequest("Aktuelles Passwort ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Neues Passwort ist erforderlich.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == parsedUserId);
        if (user is null || !user.IsActive)
        {
            return NotFound("Benutzer wurde nicht gefunden.");
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!validPassword)
        {
            return BadRequest("Aktuelles Passwort ist falsch.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            return BadRequest("Das neue Passwort muss sich vom bisherigen unterscheiden.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
