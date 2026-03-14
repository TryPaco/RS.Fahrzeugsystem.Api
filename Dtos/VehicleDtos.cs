namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateVehicleRequest(
    Guid CustomerId,
    string InternalNumber,
    string? Fin,
    string? LicensePlate,
    string Brand,
    string Model,
    string? ModelVariant,
    int? BuildYear,
    string? EngineCode,
    string? Transmission,
    string? FuelType,
    string? Color,
    long CurrentKm,
    int? StockPowerHp,
    int? CurrentPowerHp,
    string? SoftwareStage,
    string? Notes);

public sealed record UpdateVehicleRequest(
    string? Fin,
    string? LicensePlate,
    string Brand,
    string Model,
    string? ModelVariant,
    int? BuildYear,
    string? EngineCode,
    string? Transmission,
    string? FuelType,
    string? Color,
    long CurrentKm,
    int? StockPowerHp,
    int? CurrentPowerHp,
    string? SoftwareStage,
    string? Notes,
    bool IsArchived);
