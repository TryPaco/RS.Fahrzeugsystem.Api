using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/labels")]
public sealed class LabelsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission("labels.view")]
    public async Task<ActionResult> GetAll()
    {
        var items = await dbContext.Labels
            .Include(x => x.Vehicle)
            .OrderBy(x => x.Prefix)
            .ThenBy(x => x.CodeNumber)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("lookup/{code}")]
    [HasPermission("labels.view")]
    public async Task<ActionResult> Lookup(string code)
    {
        var item = await dbContext.Labels
            .Include(x => x.Vehicle)
                .ThenInclude(x => x!.Customer)
            .SingleOrDefaultAsync(x => x.Code == code);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermission("labels.manage")]
    public async Task<ActionResult> Create([FromBody] CreateLabelRequest request)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (!normalizedCode.StartsWith("RS-") && !normalizedCode.StartsWith("B-"))
            return BadRequest("Label muss mit RS- oder B- beginnen.");

        if (await dbContext.Labels.AnyAsync(x => x.Code == normalizedCode))
            return Conflict("Label existiert bereits.");

        var parts = normalizedCode.Split('-', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var codeNumber))
            return BadRequest("Ungültiges Label-Format.");

        var entity = new Label
        {
            Code = normalizedCode,
            Prefix = parts[0] + "-",
            CodeNumber = codeNumber,
            Type = request.Type,
            Notes = request.Notes,
            Status = LabelStatus.Free
        };

        dbContext.Labels.Add(entity);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(Lookup), new { code = entity.Code }, entity);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("labels.manage")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateLabelRequest request)
    {
        var entity = await dbContext.Labels.FindAsync(id);
        if (entity is null) return NotFound();

        entity.Type = request.Type;
        entity.Status = request.Status;
        entity.PositionOnVehicle = request.PositionOnVehicle;
        entity.Notes = request.Notes;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/assign")]
    [HasPermission("labels.assign")]
    public async Task<ActionResult> Assign(Guid id, [FromBody] AssignLabelRequest request)
    {
        var label = await dbContext.Labels.FindAsync(id);
        if (label is null) return NotFound("Label nicht gefunden.");
        if (label.Status == LabelStatus.Disabled) return BadRequest("Label ist deaktiviert.");
        if (label.VehicleId is not null) return Conflict("Label ist bereits zugewiesen.");

        var vehicle = await dbContext.Vehicles.FindAsync(request.VehicleId);
        if (vehicle is null) return BadRequest("Fahrzeug nicht gefunden.");

        label.VehicleId = request.VehicleId;
        label.PositionOnVehicle = request.PositionOnVehicle;
        label.Notes = request.Notes;
        label.AssignedAtUtc = DateTime.UtcNow;
        label.Status = LabelStatus.Assigned;
        label.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/unassign")]
    [HasPermission("labels.assign")]
    public async Task<ActionResult> Unassign(Guid id)
    {
        var label = await dbContext.Labels.FindAsync(id);
        if (label is null) return NotFound();

        label.VehicleId = null;
        label.PositionOnVehicle = null;
        label.AssignedAtUtc = null;
        label.Status = LabelStatus.Free;
        label.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
