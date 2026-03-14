namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateVehiclePartRequest(
    Guid? CategoryId,
    string Name,
    string? Manufacturer,
    string? PartNumber,
    string? SerialNumber,
    DateTime? InstalledAtUtc,
    long InstalledKm,
    decimal? PriceNet,
    decimal? PriceGross,
    string? Notes);

public sealed record UpdateVehiclePartRequest(
    Guid? CategoryId,
    string Name,
    string? Manufacturer,
    string? PartNumber,
    string? SerialNumber,
    DateTime? InstalledAtUtc,
    long InstalledKm,
    DateTime? RemovedAtUtc,
    long? RemovedKm,
    string Status,
    decimal? PriceNet,
    decimal? PriceGross,
    string? Notes);
