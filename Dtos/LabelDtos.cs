using RS.Fahrzeugsystem.Api.Models;

namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateLabelRequest(
    string Code,
    LabelType Type,
    string? Notes);

public sealed record AssignLabelRequest(
    Guid VehicleId,
    string? PositionOnVehicle,
    string? Notes);

public sealed record UpdateLabelRequest(
    LabelType Type,
    LabelStatus Status,
    string? PositionOnVehicle,
    string? Notes);
