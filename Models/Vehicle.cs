namespace RS.Fahrzeugsystem.Api.Models;

public sealed class Vehicle : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string InternalNumber { get; set; } = string.Empty;
    public string? Fin { get; set; }
    public string? LicensePlate { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
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

    public ICollection<Label> Labels { get; set; } = new List<Label>();
    public ICollection<VehiclePart> Parts { get; set; } = new List<VehiclePart>();
    public ICollection<VehicleHistory> HistoryEntries { get; set; } = new List<VehicleHistory>();
}
