using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/vehicle-catalog")]
public sealed class VehicleCatalogController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission("vehiclecatalog.view")]
    public async Task<ActionResult<IReadOnlyList<VehicleCatalogEntryResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? q = null)
    {
        var query = dbContext.VehicleCatalogEntries.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x =>
                x.Brand.ToLower().Contains(term) ||
                x.Model.ToLower().Contains(term) ||
                (x.Variant != null && x.Variant.ToLower().Contains(term)) ||
                (x.YearLabel != null && x.YearLabel.ToLower().Contains(term)) ||
                (x.Engine != null && x.Engine.ToLower().Contains(term)) ||
                (x.EngineCode != null && x.EngineCode.ToLower().Contains(term)) ||
                (x.Transmission != null && x.Transmission.ToLower().Contains(term)) ||
                (x.TransmissionCode != null && x.TransmissionCode.ToLower().Contains(term)) ||
                (x.EcuType != null && x.EcuType.ToLower().Contains(term)) ||
                (x.EcuManufacturer != null && x.EcuManufacturer.ToLower().Contains(term)) ||
                (x.DriveType != null && x.DriveType.ToLower().Contains(term)) ||
                (x.Platform != null && x.Platform.ToLower().Contains(term)) ||
                (x.Notes != null && x.Notes.ToLower().Contains(term))
            );
        }

        var items = await query
            .OrderBy(x => x.Brand)
            .ThenBy(x => x.Model)
            .ThenBy(x => x.Variant)
            .ThenBy(x => x.YearLabel)
            .ThenBy(x => x.Engine)
            .ThenBy(x => x.EngineCode)
            .Select(x => MapResponse(x))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("vehiclecatalog.view")]
    public async Task<ActionResult<VehicleCatalogEntryResponse>> GetById(Guid id)
    {
        var item = await dbContext.VehicleCatalogEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return Ok(MapResponse(item));
    }

    [HttpPost]
    [HasPermission("vehiclecatalog.manage")]
    public async Task<ActionResult<VehicleCatalogEntryResponse>> Create(
        [FromBody] CreateVehicleCatalogEntryRequest request)
    {
        var validationError = ValidateRequest(request.Brand, request.Model, request.BuildYearFrom, request.BuildYearTo);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var entity = new VehicleCatalogEntry
        {
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            Variant = Normalize(request.Variant),
            YearLabel = Normalize(request.YearLabel),
            BuildYearFrom = request.BuildYearFrom,
            BuildYearTo = request.BuildYearTo,
            Engine = Normalize(request.Engine),
            EngineCode = Normalize(request.EngineCode),
            Transmission = Normalize(request.Transmission),
            TransmissionCode = Normalize(request.TransmissionCode),
            EcuType = Normalize(request.EcuType),
            EcuManufacturer = Normalize(request.EcuManufacturer),
            DriveType = Normalize(request.DriveType),
            Platform = Normalize(request.Platform),
            Notes = Normalize(request.Notes),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.VehicleCatalogEntries.Add(entity);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapResponse(entity));
    }

    [HttpPut("{id:guid}")]
    [HasPermission("vehiclecatalog.manage")]
    public async Task<ActionResult<VehicleCatalogEntryResponse>> Update(
        Guid id,
        [FromBody] UpdateVehicleCatalogEntryRequest request)
    {
        var validationError = ValidateRequest(request.Brand, request.Model, request.BuildYearFrom, request.BuildYearTo);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var entity = await dbContext.VehicleCatalogEntries.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Brand = request.Brand.Trim();
        entity.Model = request.Model.Trim();
        entity.Variant = Normalize(request.Variant);
        entity.YearLabel = Normalize(request.YearLabel);
        entity.BuildYearFrom = request.BuildYearFrom;
        entity.BuildYearTo = request.BuildYearTo;
        entity.Engine = Normalize(request.Engine);
        entity.EngineCode = Normalize(request.EngineCode);
        entity.Transmission = Normalize(request.Transmission);
        entity.TransmissionCode = Normalize(request.TransmissionCode);
        entity.EcuType = Normalize(request.EcuType);
        entity.EcuManufacturer = Normalize(request.EcuManufacturer);
        entity.DriveType = Normalize(request.DriveType);
        entity.Platform = Normalize(request.Platform);
        entity.Notes = Normalize(request.Notes);
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return Ok(MapResponse(entity));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("vehiclecatalog.manage")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var entity = await dbContext.VehicleCatalogEntries.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        dbContext.VehicleCatalogEntries.Remove(entity);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValidateRequest(string brand, string model, int? buildYearFrom, int? buildYearTo)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return "Brand ist erforderlich.";
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return "Model ist erforderlich.";
        }

        if (buildYearFrom.HasValue && buildYearTo.HasValue && buildYearFrom > buildYearTo)
        {
            return "BuildYearFrom darf nicht größer als BuildYearTo sein.";
        }

        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VehicleCatalogEntryResponse MapResponse(VehicleCatalogEntry x) =>
        new(
            x.Id,
            x.Brand,
            x.Model,
            x.Variant,
            x.YearLabel,
            x.BuildYearFrom,
            x.BuildYearTo,
            x.Engine,
            x.EngineCode,
            x.Transmission,
            x.TransmissionCode,
            x.EcuType,
            x.EcuManufacturer,
            x.DriveType,
            x.Platform,
            x.Notes,
            x.IsActive,
            x.CreatedAtUtc,
            x.UpdatedAtUtc);
}
