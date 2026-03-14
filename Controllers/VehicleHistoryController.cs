using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Extensions;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class VehicleHistoryController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("vehicles/{vehicleId:guid}/history")]
    [HasPermission("history.view")]
    public async Task<ActionResult> GetForVehicle(Guid vehicleId)
    {
        var history = await dbContext.VehicleHistory
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.EventDateUtc)
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost("vehicles/{vehicleId:guid}/history")]
    [HasPermission("history.create")]
    public async Task<ActionResult> Create(Guid vehicleId, [FromBody] CreateHistoryRequest request)
    {
        if (request.KmValue <= 0)
            return BadRequest("KM-Stand ist Pflicht und muss größer als 0 sein.");

        if (!await dbContext.Vehicles.AnyAsync(x => x.Id == vehicleId))
            return BadRequest("Fahrzeug nicht gefunden.");

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var entity = new VehicleHistory
        {
            VehicleId = vehicleId,
            EventType = request.EventType,
            Title = request.Title,
            Description = request.Description,
            EventDateUtc = request.EventDateUtc ?? DateTime.UtcNow,
            KmRequired = true,
            KmValue = request.KmValue,
            CreatedByUserId = userId.Value
        };

        dbContext.VehicleHistory.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut("history/{id:guid}")]
    [HasPermission("history.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateHistoryRequest request)
    {
        if (request.KmValue <= 0)
            return BadRequest("KM-Stand ist Pflicht und muss größer als 0 sein.");

        var entity = await dbContext.VehicleHistory.FindAsync(id);
        if (entity is null) return NotFound();

        entity.EventType = request.EventType;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.EventDateUtc = request.EventDateUtc ?? DateTime.UtcNow;
        entity.KmValue = request.KmValue;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("history/{id:guid}")]
    [HasPermission("history.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var entity = await dbContext.VehicleHistory.FindAsync(id);
        if (entity is null) return NotFound();

        dbContext.VehicleHistory.Remove(entity);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
