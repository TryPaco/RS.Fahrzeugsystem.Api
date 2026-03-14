using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RS.Fahrzeugsystem.Api.Authorization;
using RS.Fahrzeugsystem.Api.Data;
using RS.Fahrzeugsystem.Api.Dtos;
using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(AppDbContext dbContext) : ControllerBase
{
	[HttpGet]
	[HasPermission("customers.view")]
	public async Task<ActionResult> GetAll([FromQuery] bool includeArchived = false)
	{
		var query = dbContext.Customers.AsQueryable();

		if (!includeArchived)
			query = query.Where(x => !x.IsArchived);

		var customers = await query
			.OrderBy(x => x.LastName)
			.ThenBy(x => x.FirstName)
			.Select(x => new
			{
				x.Id,
				x.CustomerNumber,
				x.CompanyName,
				x.FirstName,
				x.LastName,
				x.Phone,
				x.Email,
				x.City,
				x.IsArchived
			})
			.ToListAsync();

		return Ok(customers);
	}

	[HttpGet("{id:guid}")]
	[HasPermission("customers.view")]
	public async Task<ActionResult> GetById(Guid id)
	{
		var customer = await dbContext.Customers
			.Include(x => x.Vehicles)
			.SingleOrDefaultAsync(x => x.Id == id);

		if (customer is null)
			return NotFound();

		return Ok(customer);
	}

	[HttpPost]
	[HasPermission("customers.create")]
	public async Task<ActionResult> Create([FromBody] CreateCustomerRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.CustomerNumber))
			return BadRequest("CustomerNumber ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.FirstName))
			return BadRequest("FirstName ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.LastName))
			return BadRequest("LastName ist erforderlich.");

		if (await dbContext.Customers.AnyAsync(x => x.CustomerNumber == request.CustomerNumber))
			return Conflict("Kundennummer existiert bereits.");

		var entity = new Customer
		{
			Id = Guid.NewGuid(),
			CustomerNumber = request.CustomerNumber.Trim(),
			CompanyName = request.CompanyName?.Trim(),
			FirstName = request.FirstName.Trim(),
			LastName = request.LastName.Trim(),
			Phone = request.Phone?.Trim(),
			Email = request.Email?.Trim(),
			Street = request.Street?.Trim(),
			ZipCode = request.ZipCode?.Trim(),
			City = request.City?.Trim(),
			Country = request.Country?.Trim(),
			Notes = request.Notes?.Trim(),
			CreatedAtUtc = DateTime.UtcNow,
			IsArchived = false
		};

		dbContext.Customers.Add(entity);
		await dbContext.SaveChangesAsync();

		return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
	}

	[HttpPut("{id:guid}")]
	[HasPermission("customers.edit")]
	public async Task<ActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
	{
		var entity = await dbContext.Customers.FindAsync(id);

		if (entity is null)
			return NotFound();

		if (string.IsNullOrWhiteSpace(request.CustomerNumber))
			return BadRequest("CustomerNumber ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.FirstName))
			return BadRequest("FirstName ist erforderlich.");

		if (string.IsNullOrWhiteSpace(request.LastName))
			return BadRequest("LastName ist erforderlich.");

		if (await dbContext.Customers.AnyAsync(x => x.Id != id && x.CustomerNumber == request.CustomerNumber))
			return Conflict("Kundennummer existiert bereits.");

		entity.CustomerNumber = request.CustomerNumber.Trim();
		entity.CompanyName = request.CompanyName?.Trim();
		entity.FirstName = request.FirstName.Trim();
		entity.LastName = request.LastName.Trim();
		entity.Phone = request.Phone?.Trim();
		entity.Email = request.Email?.Trim();
		entity.Street = request.Street?.Trim();
		entity.ZipCode = request.ZipCode?.Trim();
		entity.City = request.City?.Trim();
		entity.Country = request.Country?.Trim();
		entity.Notes = request.Notes?.Trim();
		entity.IsArchived = request.IsArchived;
		entity.UpdatedAtUtc = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();

		return Ok(entity);
	}

	[HttpDelete("{id:guid}")]
	[HasPermission("customers.delete")]
	public async Task<ActionResult> Archive(Guid id)
	{
		var entity = await dbContext.Customers.FindAsync(id);

		if (entity is null)
			return NotFound();

		entity.IsArchived = true;
		entity.UpdatedAtUtc = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();

		return NoContent();
	}
}
