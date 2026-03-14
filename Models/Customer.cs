using System.ComponentModel.DataAnnotations;

namespace RS.Fahrzeugsystem.Api.Models;

public class Customer
{
	[Key]
	public Guid Id { get; set; }

	public string CustomerNumber { get; set; } = default!;

	public string? CompanyName { get; set; }

	public string FirstName { get; set; } = default!;

	public string LastName { get; set; } = default!;

	public string? Phone { get; set; }

	public string? Email { get; set; }

	public string? Street { get; set; }

	public string? ZipCode { get; set; }

	public string? City { get; set; }

	public string? Country { get; set; }

	public string? Notes { get; set; }

	public bool IsArchived { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? UpdatedAtUtc { get; set; }

	// 🔴 wichtig
	public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
