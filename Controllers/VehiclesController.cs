using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [HasPermission("vehicles.view")]
    public async Task<ActionResult> GetAll([FromQuery] bool includeArchived = false)
    {
        var query = dbContext.Vehicles.Include(x => x.Customer).AsQueryable();
        if (!includeArchived)
            query = query.Where(x => !x.IsArchived);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.InternalNumber,
                x.Fin,
                x.LicensePlate,
                x.Brand,
                x.Model,
                x.CurrentKm,
                Customer = x.Customer.FirstName + " " + x.Customer.LastName,
                x.IsArchived
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("vehicles.view")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var vehicle = await dbContext.Vehicles
            .Include(x => x.Customer)
            .Include(x => x.Labels)
            .Include(x => x.Parts)
            .Include(x => x.HistoryEntries)
            .SingleOrDefaultAsync(x => x.Id == id);

        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpGet("search")]
    [HasPermission("vehicles.view")]
    public async Task<ActionResult> Search([FromQuery] string q)
    {
        q = q.Trim();
        var results = await dbContext.Vehicles
            .Include(x => x.Customer)
            .Where(x => !x.IsArchived && (
                (x.InternalNumber != null && x.InternalNumber.Contains(q)) ||
                (x.Fin != null && x.Fin.Contains(q)) ||
                (x.LicensePlate != null && x.LicensePlate.Contains(q)) ||
                x.Brand.Contains(q) ||
                x.Model.Contains(q) ||
                x.Customer.FirstName.Contains(q) ||
                x.Customer.LastName.Contains(q)))
            .Take(50)
            .ToListAsync();

        return Ok(results);
    }

    [HttpPost]
    [HasPermission("vehicles.create")]
    public async Task<ActionResult> Create([FromBody] CreateVehicleRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Fin) && await dbContext.Vehicles.AnyAsync(x => x.Fin == request.Fin))
            return Conflict("FIN existiert bereits.");

        if (!await dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId))
            return BadRequest("Kunde nicht gefunden.");

        var entity = new Vehicle
        {
            CustomerId = request.CustomerId,
            InternalNumber = request.InternalNumber,
            Fin = request.Fin,
            LicensePlate = request.LicensePlate,
            Brand = request.Brand,
            Model = request.Model,
            ModelVariant = request.ModelVariant,
            BuildYear = request.BuildYear,
            EngineCode = request.EngineCode,
            Transmission = request.Transmission,
            FuelType = request.FuelType,
            Color = request.Color,
            CurrentKm = request.CurrentKm,
            StockPowerHp = request.StockPowerHp,
            CurrentPowerHp = request.CurrentPowerHp,
            SoftwareStage = request.SoftwareStage,
            Notes = request.Notes
        };

        dbContext.Vehicles.Add(entity);
        await dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("vehicles.edit")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request)
    {
        var entity = await dbContext.Vehicles.FindAsync(id);
        if (entity is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.Fin) && request.Fin != entity.Fin && await dbContext.Vehicles.AnyAsync(x => x.Fin == request.Fin))
            return Conflict("FIN existiert bereits.");

        entity.Fin = request.Fin;
        entity.LicensePlate = request.LicensePlate;
        entity.Brand = request.Brand;
        entity.Model = request.Model;
        entity.ModelVariant = request.ModelVariant;
        entity.BuildYear = request.BuildYear;
        entity.EngineCode = request.EngineCode;
        entity.Transmission = request.Transmission;
        entity.FuelType = request.FuelType;
        entity.Color = request.Color;
        entity.CurrentKm = request.CurrentKm;
        entity.StockPowerHp = request.StockPowerHp;
        entity.CurrentPowerHp = request.CurrentPowerHp;
        entity.SoftwareStage = request.SoftwareStage;
        entity.Notes = request.Notes;
        entity.IsArchived = request.IsArchived;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("vehicles.delete")]
    public async Task<ActionResult> Archive(Guid id)
    {
        var entity = await dbContext.Vehicles.FindAsync(id);
        if (entity is null) return NotFound();

        entity.IsArchived = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
