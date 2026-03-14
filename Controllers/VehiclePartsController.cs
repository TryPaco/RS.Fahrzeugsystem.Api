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
public sealed class VehiclePartsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("vehicles/{vehicleId:guid}/parts")]
    [HasPermission("parts.view")]
    public async Task<ActionResult> GetForVehicle(Guid vehicleId)
    {
        var parts = await dbContext.VehicleParts.Where(x => x.VehicleId == vehicleId).ToListAsync();
        return Ok(parts);
    }

    [HttpPost("vehicles/{vehicleId:guid}/parts")]
    [HasPermission("parts.create")]
    public async Task<ActionResult> Create(Guid vehicleId, [FromBody] CreateVehiclePartRequest request)
    {
        if (request.InstalledKm <= 0)
            return BadRequest("KM-Stand ist Pflicht und muss größer als 0 sein.");

        if (!await dbContext.Vehicles.AnyAsync(x => x.Id == vehicleId))
            return BadRequest("Fahrzeug nicht gefunden.");

        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized();

        var entity = new VehiclePart
        {
            VehicleId = vehicleId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Manufacturer = request.Manufacturer,
            PartNumber = request.PartNumber,
            SerialNumber = request.SerialNumber,
            InstalledAtUtc = request.InstalledAtUtc,
            InstalledKm = request.InstalledKm,
            PriceNet = request.PriceNet,
            PriceGross = request.PriceGross,
            Notes = request.Notes,
            CreatedByUserId = userId.Value
        };

        dbContext.VehicleParts.Add(entity);
        await dbContext.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpPut("parts/{id:guid}")]
    [HasPermission("parts.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateVehiclePartRequest request)
    {
        if (request.InstalledKm <= 0)
            return BadRequest("KM-Stand ist Pflicht und muss größer als 0 sein.");

        var entity = await dbContext.VehicleParts.FindAsync(id);
        if (entity is null) return NotFound();

        entity.CategoryId = request.CategoryId;
        entity.Name = request.Name;
        entity.Manufacturer = request.Manufacturer;
        entity.PartNumber = request.PartNumber;
        entity.SerialNumber = request.SerialNumber;
        entity.InstalledAtUtc = request.InstalledAtUtc;
        entity.InstalledKm = request.InstalledKm;
        entity.RemovedAtUtc = request.RemovedAtUtc;
        entity.RemovedKm = request.RemovedKm;
        entity.Status = request.Status;
        entity.PriceNet = request.PriceNet;
        entity.PriceGross = request.PriceGross;
        entity.Notes = request.Notes;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("parts/{id:guid}")]
    [HasPermission("parts.delete")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var entity = await dbContext.VehicleParts.FindAsync(id);
        if (entity is null) return NotFound();

        dbContext.VehicleParts.Remove(entity);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
