namespace RS.Fahrzeugsystem.Api.Models;

public sealed class VehicleCatalogEntry : BaseEntity
{
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public string? YearLabel { get; set; }
    public int? BuildYearFrom { get; set; }
    public int? BuildYearTo { get; set; }
    public string? Engine { get; set; }
    public string? EngineCode { get; set; }
    public string? Transmission { get; set; }
    public string? TransmissionCode { get; set; }
    public string? EcuType { get; set; }
    public string? EcuManufacturer { get; set; }
    public string? DriveType { get; set; }
    public string? Platform { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
