using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public sealed class VehiclesController(AppDbContext dbContext) : ControllerBase
{
	public sealed class CreateVehicleRequest
	{
		public Guid CustomerId { get; set; }
		public string InternalNumber { get; set; } = default!;
		public string? Fin { get; set; }
		public string? LicensePlate { get; set; }
		public string Brand { get; set; } = default!;
		public string Model { get; set; } = default!;
		public string? ModelVariant { get; set; }
		public int? BuildYear { get; set; }
		public string? EngineCode { get; set; }
		public string? Transmission { get; set; }
		public string? FuelType { get; set; }
		public string? Color { get; set; }
		public long CurrentKm { get; set; }
		public int? StockPowerHp { get; set; }
		public int? CurrentPowerHp { get; set; }
		public string? SoftwareStage { get; set; }
		public string? Notes { get; set; }
	}

	public sealed class UpdateVehicleRequest
	{
		public Guid CustomerId { get; set; }
		public string InternalNumber { get; set; } = default!;
		public string? Fin { get; set; }
		public string? LicensePlate { get; set; }
		public string Brand { get; set; } = default!;
		public string Model { get; set; } = default!;
		public string? ModelVariant { get; set; }
		public int? BuildYear { get; set; }
		public string? EngineCode { get; set; }
		public string? Transmission { get; set; }
		public string? FuelType { get; set; }
		public string? Color { get; set; }
		public long CurrentKm { get; set; }
		public int? StockPowerHp { get; set; }
		public int? CurrentPowerHp { get; set; }
		public string? SoftwareStage { get; set; }
		public string? Notes { get; set; }
		public bool IsArchived { get; set; }
	}

	[HttpGet]
	[HasPermission("vehicles.view")]
	public async Task<ActionResult> GetAll(
		[FromQuery] bool includeArchived = false,
		[FromQuery] string? q = null)
	{
		var query = dbContext.Vehicles
			.AsNoTracking()
			.Include(x => x.Customer)
			.AsQueryable();

		if (!includeArchived)
			query = query.Where(x => !x.IsArchived);

		if (!string.IsNullOrWhiteSpace(q))
		{
			var term = q.Trim().ToLower();

			query = query.Where(x =>
				x.InternalNumber.ToLower().Contains(term) ||
				(x.Fin != null && x.Fin.ToLower().Contains(term)) ||
				(x.LicensePlate != null && x.LicensePlate.ToLower().Contains(term)) ||
				x.Brand.ToLower().Contains(term) ||
				x.Model.ToLower().Contains(term) ||
				(x.ModelVariant != null && x.ModelVariant.ToLower().Contains(term)) ||
				x.Customer.FirstName.ToLower().Contains(term) ||
				x.Customer.LastName.ToLower().Contains(term) ||
				(x.Customer.CompanyName != null && x.Customer.CompanyName.ToLower().Contains(term))
			);
		}

		var vehicles = await query
			.OrderBy(x => x.Brand)
			.ThenBy(x => x.Model)
			.ThenBy(x => x.LicensePlate)
			.Select(x => new
			{
				x.Id,
				x.CustomerId,
				customerName = x.Customer.CompanyName != null && x.Customer.CompanyName != ""
					? x.Customer.CompanyName
					: x.Customer.FirstName + " " + x.Customer.LastName,
				x.InternalNumber,
				x.Fin,
				x.LicensePlate,
				x.Brand,
				x.Model,
				x.ModelVariant,
				x.BuildYear,
				x.EngineCode,
				x.Transmission,
				x.FuelType,
				x.Color,
				x.CurrentKm,
				x.StockPowerHp,
				x.CurrentPowerHp,
				x.SoftwareStage,
				x.Notes,
				x.IsArchived,
				x.CreatedAtUtc,
				x.UpdatedAtUtc
			})
			.ToListAsync();

		return Ok(vehicles);
	}

	[HttpGet("{id:guid}")]
	[HasPermission("vehicles.view")]
	public async Task<ActionResult> GetById(Guid id)
	{
		var vehicle = await dbContext.Vehicles
			.AsNoTracking()
			.Include(x => x.Customer)
			.Include(x => x.Labels)
			.Include(x => x.Parts)
			.Include(x => x.HistoryEntries)
			.SingleOrDefaultAsync(x => x.Id == id);

		if (vehicle is null)
			return NotFound();

		return Ok(new
		{
			vehicle.Id,
			vehicle.CustomerId,
			customer = new
			{
				vehicle.Customer.Id,
				vehicle.Customer.CustomerNumber,
				vehicle.Customer.CompanyName,
				vehicle.Customer.FirstName,
				vehicle.Customer.LastName,
				vehicle.Customer.Phone,
				vehicle.Customer.Email
			},
			vehicle.InternalNumber,
			vehicle.Fin,
			vehicle.LicensePlate,
			vehicle.Brand,
			vehicle.Model,
			vehicle.ModelVariant,
			vehicle.BuildYear,
			vehicle.EngineCode,
			vehicle.Transmission,
			vehicle.FuelType,
			vehicle.Color,
			vehicle.CurrentKm,
			vehicle.StockPowerHp,
			vehicle.CurrentPowerHp,
			vehicle.SoftwareStage,
			vehicle.Notes,
			vehicle.IsArchived,
			vehicle.CreatedAtUtc,
			vehicle.UpdatedAtUtc,
			labels = vehicle.Labels
				.Select(l => new
				{
					l.Id,
					l.Code,
					l.Prefix,
					l.CodeNumber,
					l.Type,
					l.Status,
					l.PositionOnVehicle,
					l.AssignedAtUtc,
					l.Notes
				})
				.OrderBy(l => l.Code)
				.ToList(),
			parts = vehicle.Parts
				.Select(p => new
				{
					p.Id,
					p.CategoryId,
					p.Name,
					p.Manufacturer,
					p.PartNumber,
					p.SerialNumber,
					p.InstalledAtUtc,
					p.InstalledKm,
					p.RemovedAtUtc,
					p.RemovedKm,
					p.Status,
					p.PriceNet,
					p.PriceGross,
					p.Notes,
					p.CreatedAtUtc,
					p.UpdatedAtUtc
				})
				.OrderByDescending(p => p.InstalledAtUtc)
				.ToList(),
			history = vehicle.HistoryEntries
				.Select(h => new
				{
					h.Id,
					h.EventType,
					h.Title,
					h.Description,
					h.EventDateUtc,
					h.KmRequired,
					h.KmValue,
					h.CreatedByUserId,
					h.CreatedAtUtc,
					h.UpdatedAtUtc
				})
				.OrderByDescending(h => h.EventDateUtc)
				.ToList()
		});
	}

	[HttpPost]
	[HasPermission("vehicles.create")]
	public async Task<ActionResult> Create([FromBody] CreateVehicleRequest request)
	{
		if (request.CustomerId == Guid.Empty)
			return BadRequest("CustomerId ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.InternalNumber))
			return BadRequest("InternalNumber ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.Brand))
			return BadRequest("Brand ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.Model))
			return BadRequest("Model ist erforderlich.");

		if (request.CurrentKm < 0)
			return BadRequest("CurrentKm darf nicht kleiner als 0 sein.");

		var customerExists = await dbContext.Customers
			.AnyAsync(x => x.Id == request.CustomerId && !x.IsArchived);

		if (!customerExists)
			return BadRequest("Der angegebene Kunde existiert nicht oder ist archiviert.");

		var internalNumber = request.InternalNumber.Trim();
		var fin = string.IsNullOrWhiteSpace(request.Fin) ? null : request.Fin.Trim().ToUpperInvariant();
		var licensePlate = string.IsNullOrWhiteSpace(request.LicensePlate) ? null : request.LicensePlate.Trim().ToUpperInvariant();

		if (await dbContext.Vehicles.AnyAsync(x => x.InternalNumber == internalNumber))
			return Conflict("Die interne Fahrzeugnummer existiert bereits.");

		if (!string.IsNullOrWhiteSpace(fin) &&
			await dbContext.Vehicles.AnyAsync(x => x.Fin != null && x.Fin == fin))
			return Conflict("Die FIN existiert bereits.");

		var entity = new Vehicle
		{
			Id = Guid.NewGuid(),
			CustomerId = request.CustomerId,
			InternalNumber = internalNumber,
			Fin = fin,
			LicensePlate = licensePlate,
			Brand = request.Brand.Trim(),
			Model = request.Model.Trim(),
			ModelVariant = request.ModelVariant?.Trim(),
			BuildYear = request.BuildYear,
			EngineCode = request.EngineCode?.Trim(),
			Transmission = request.Transmission?.Trim(),
			FuelType = request.FuelType?.Trim(),
			Color = request.Color?.Trim(),
			CurrentKm = request.CurrentKm,
			StockPowerHp = request.StockPowerHp,
			CurrentPowerHp = request.CurrentPowerHp,
			SoftwareStage = request.SoftwareStage?.Trim(),
			Notes = request.Notes?.Trim(),
			IsArchived = false,
			CreatedAtUtc = DateTime.UtcNow
		};

		dbContext.Vehicles.Add(entity);
		await dbContext.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new
		{
			entity.Id,
			entity.CustomerId,
			entity.InternalNumber,
			entity.Fin,
			entity.LicensePlate,
			entity.Brand,
			entity.Model,
			entity.ModelVariant,
			entity.BuildYear,
			entity.EngineCode,
			entity.Transmission,
			entity.FuelType,
			entity.Color,
			entity.CurrentKm,
			entity.StockPowerHp,
			entity.CurrentPowerHp,
			entity.SoftwareStage,
			entity.Notes,
			entity.IsArchived,
			entity.CreatedAtUtc,
			entity.UpdatedAtUtc
		});
	}

	[HttpPut("{id:guid}")]
	[HasPermission("vehicles.edit")]
	public async Task<ActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request)
	{
		var entity = await dbContext.Vehicles.FindAsync(id);
		if (entity is null)
			return NotFound();

		if (request.CustomerId == Guid.Empty)
			return BadRequest("CustomerId ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.InternalNumber))
			return BadRequest("InternalNumber ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.Brand))
			return BadRequest("Brand ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.Model))
			return BadRequest("Model ist erforderlich.");

		if (request.CurrentKm < 0)
			return BadRequest("CurrentKm darf nicht kleiner als 0 sein.");

		var customerExists = await dbContext.Customers
			.AnyAsync(x => x.Id == request.CustomerId && !x.IsArchived);

		if (!customerExists)
			return BadRequest("Der angegebene Kunde existiert nicht oder ist archiviert.");

		var internalNumber = request.InternalNumber.Trim();
		var fin = string.IsNullOrWhiteSpace(request.Fin) ? null : request.Fin.Trim().ToUpperInvariant();
		var licensePlate = string.IsNullOrWhiteSpace(request.LicensePlate) ? null : request.LicensePlate.Trim().ToUpperInvariant();

		if (await dbContext.Vehicles.AnyAsync(x => x.Id != id && x.InternalNumber == internalNumber))
			return Conflict("Die interne Fahrzeugnummer existiert bereits.");

		if (!string.IsNullOrWhiteSpace(fin) &&
			await dbContext.Vehicles.AnyAsync(x => x.Id != id && x.Fin != null && x.Fin == fin))
			return Conflict("Die FIN existiert bereits.");

		entity.CustomerId = request.CustomerId;
		entity.InternalNumber = internalNumber;
		entity.Fin = fin;
		entity.LicensePlate = licensePlate;
		entity.Brand = request.Brand.Trim();
		entity.Model = request.Model.Trim();
		entity.ModelVariant = request.ModelVariant?.Trim();
		entity.BuildYear = request.BuildYear;
		entity.EngineCode = request.EngineCode?.Trim();
		entity.Transmission = request.Transmission?.Trim();
		entity.FuelType = request.FuelType?.Trim();
		entity.Color = request.Color?.Trim();
		entity.CurrentKm = request.CurrentKm;
		entity.StockPowerHp = request.StockPowerHp;
		entity.CurrentPowerHp = request.CurrentPowerHp;
		entity.SoftwareStage = request.SoftwareStage?.Trim();
		entity.Notes = request.Notes?.Trim();
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
		if (entity is null)
			return NotFound();

		entity.IsArchived = true;
		entity.UpdatedAtUtc = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();
		return NoContent();
	}
}