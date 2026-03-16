using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;
using RS.Fahrzeugsystem.Api.Options;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Superadmin")]
public sealed class SettingsController(
    AppDbContext dbContext,
    IOptions<SmtpOptions> smtpOptions) : ControllerBase
{
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;

    [HttpGet("smtp")]
    public async Task<ActionResult<SmtpSettingsResponse>> GetSmtpSettings()
    {
        var settings = await dbContext.SmtpConfigurations
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            return Ok(new SmtpSettingsResponse(
                _smtpOptions.Host,
                _smtpOptions.Port,
                _smtpOptions.Username,
                _smtpOptions.FromEmail,
                _smtpOptions.FromName,
                _smtpOptions.EnableSsl,
                !string.IsNullOrWhiteSpace(_smtpOptions.Password)));
        }

        return Ok(new SmtpSettingsResponse(
            settings.Host,
            settings.Port,
            settings.Username,
            settings.FromEmail,
            settings.FromName,
            settings.EnableSsl,
            !string.IsNullOrWhiteSpace(settings.Password)));
    }

    [HttpPut("smtp")]
    public async Task<ActionResult> UpdateSmtpSettings([FromBody] UpdateSmtpSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Host))
            return BadRequest("SMTP-Host ist erforderlich.");

        if (request.Port <= 0)
            return BadRequest("SMTP-Port ist ungültig.");

        if (string.IsNullOrWhiteSpace(request.FromEmail))
            return BadRequest("Absender-E-Mail ist erforderlich.");

        var settings = await dbContext.SmtpConfigurations
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new SmtpConfiguration();
            dbContext.SmtpConfigurations.Add(settings);
        }

        settings.Host = request.Host.Trim();
        settings.Port = request.Port;
        settings.Username = request.Username?.Trim() ?? string.Empty;
        settings.FromEmail = request.FromEmail.Trim();
        settings.FromName = request.FromName?.Trim() ?? "RS Fahrzeugsystem";
        settings.EnableSsl = request.EnableSsl;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            settings.Password = request.Password;
        }

        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
