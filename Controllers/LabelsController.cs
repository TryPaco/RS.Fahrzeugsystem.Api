using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Models;
using System.Text.RegularExpressions;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/labels")]
public sealed class LabelsController(AppDbContext dbContext) : ControllerBase
{
	public sealed class CreateLabelRequest
	{
		public string Code { get; set; } = default!;
		public string? Notes { get; set; }
	}

	public sealed class AssignLabelRequest
	{
		public string Code { get; set; } = default!;
		public Guid VehicleId { get; set; }
		public string? PositionOnVehicle { get; set; }
	}

	private static readonly Regex LabelRegex = new(@"^(RS|B)-\d{6}$", RegexOptions.Compiled);

	private static bool TryParseLabelCode(string rawCode, out string normalizedCode, out string prefix, out int codeNumber)
	{
		normalizedCode = rawCode?.Trim().ToUpperInvariant() ?? string.Empty;
		prefix = string.Empty;
		codeNumber = 0;

		if (!LabelRegex.IsMatch(normalizedCode))
			return false;

		var parts = normalizedCode.Split('-', 2);
		prefix = parts[0];

		if (!int.TryParse(parts[1], out codeNumber))
			return false;

		return true;
	}

	[HttpGet]
	[HasPermission("labels.view")]
	public async Task<ActionResult> GetAll()
	{
		var labels = await dbContext.Labels
			.AsNoTracking()
			.Include(x => x.Vehicle)
			.OrderBy(x => x.Prefix)
			.ThenBy(x => x.CodeNumber)
			.Select(x => new
			{
				x.Id,
				x.Code,
				x.Prefix,
				x.CodeNumber,
				status = (int)x.Status,
				x.VehicleId,
				x.PositionOnVehicle,
				x.AssignedAtUtc,
				x.Notes,
				vehicle = x.Vehicle == null
					? null
					: new
					{
						x.Vehicle.Id,
						x.Vehicle.InternalNumber,
						x.Vehicle.LicensePlate,
						x.Vehicle.Brand,
						x.Vehicle.Model
					}
			})
			.ToListAsync();

		return Ok(labels);
	}

	[HttpGet("{code}")]
	[HasPermission("labels.view")]
	public async Task<ActionResult> GetByCode(string code)
	{
		var normalizedCode = code.Trim().ToUpperInvariant();

		var label = await dbContext.Labels
			.AsNoTracking()
			.Include(x => x.Vehicle)
				.ThenInclude(v => v!.Customer)
			.SingleOrDefaultAsync(x => x.Code == normalizedCode);

		if (label is null)
			return NotFound("Label nicht gefunden.");

		return Ok(new
		{
			label.Id,
			label.Code,
			label.Prefix,
			label.CodeNumber,
			status = (int)label.Status,
			label.VehicleId,
			label.PositionOnVehicle,
			label.AssignedAtUtc,
			label.Notes,
			vehicle = label.Vehicle == null
				? null
				: new
				{
					label.Vehicle.Id,
					label.Vehicle.InternalNumber,
					label.Vehicle.LicensePlate,
					label.Vehicle.Brand,
					label.Vehicle.Model,
					customer = label.Vehicle.Customer == null
						? null
						: new
						{
							label.Vehicle.Customer.Id,
							label.Vehicle.Customer.CustomerNumber,
							label.Vehicle.Customer.CompanyName,
							label.Vehicle.Customer.FirstName,
							label.Vehicle.Customer.LastName
						}
				}
		});
	}

	[HttpPost]
	[HasPermission("labels.manage")]
	public async Task<ActionResult> Create([FromBody] CreateLabelRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Code))
			return BadRequest("Code ist erforderlich.");

		if (!TryParseLabelCode(request.Code, out var normalizedCode, out var prefix, out var codeNumber))
			return BadRequest("Erlaubt sind nur Label-Codes wie RS-000001 oder B-000001.");

		var exists = await dbContext.Labels.AnyAsync(x => x.Code == normalizedCode);
		if (exists)
			return Conflict("Dieses Label existiert bereits.");

		var entity = new Label
		{
			Id = Guid.NewGuid(),
			Code = normalizedCode,
			Prefix = prefix,
			CodeNumber = codeNumber,
			Status = LabelStatus.Free,
			Notes = request.Notes?.Trim(),
			CreatedAtUtc = DateTime.UtcNow
		};

		dbContext.Labels.Add(entity);
		await dbContext.SaveChangesAsync();

		return Ok(new
		{
			entity.Id,
			entity.Code,
			entity.Prefix,
			entity.CodeNumber,
			status = (int)entity.Status,
			entity.VehicleId,
			entity.PositionOnVehicle,
			entity.AssignedAtUtc,
			entity.Notes
		});
	}

	[HttpPost("assign")]
	[HasPermission("labels.assign")]
	public async Task<ActionResult> Assign([FromBody] AssignLabelRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Code))
			return BadRequest("Code ist erforderlich.");

		var normalizedCode = request.Code.Trim().ToUpperInvariant();

		var label = await dbContext.Labels.SingleOrDefaultAsync(x => x.Code == normalizedCode);
		if (label is null)
			return NotFound("Label nicht gefunden.");

		var vehicle = await dbContext.Vehicles
			.SingleOrDefaultAsync(x => x.Id == request.VehicleId && !x.IsArchived);

		if (vehicle is null)
			return NotFound("Fahrzeug nicht gefunden.");

		label.VehicleId = request.VehicleId;
		label.PositionOnVehicle = request.PositionOnVehicle?.Trim();
		label.Status = LabelStatus.Assigned;
		label.AssignedAtUtc = DateTime.UtcNow;
		label.UpdatedAtUtc = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();

		return Ok(new
		{
			label.Id,
			label.Code,
			label.Prefix,
			label.CodeNumber,
			status = (int)label.Status,
			label.VehicleId,
			label.PositionOnVehicle,
			label.AssignedAtUtc,
			label.Notes
		});
	}

	[HttpPost("unassign")]
	[HasPermission("labels.assign")]
	public async Task<ActionResult> Unassign([FromQuery] string code)
	{
		var normalizedCode = code.Trim().ToUpperInvariant();

		var label = await dbContext.Labels.SingleOrDefaultAsync(x => x.Code == normalizedCode);
		if (label is null)
			return NotFound("Label nicht gefunden.");

		label.VehicleId = null;
		label.PositionOnVehicle = null;
		label.Status = LabelStatus.Free;
		label.AssignedAtUtc = null;
		label.UpdatedAtUtc = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();

		return Ok(new
		{
			label.Id,
			label.Code,
			label.Prefix,
			label.CodeNumber,
			status = (int)label.Status,
			label.VehicleId,
			label.PositionOnVehicle,
			label.AssignedAtUtc,
			label.Notes
		});
	}
}
