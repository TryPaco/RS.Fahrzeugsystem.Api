namespace RS.Fahrzeugsystem.Api.Models;

public sealed class VehiclePart : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public PartCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? InstalledAtUtc { get; set; }
    public long InstalledKm { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
    public long? RemovedKm { get; set; }
    public string Status { get; set; } = "installed";
    public decimal? PriceNet { get; set; }
    public decimal? PriceGross { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
}
