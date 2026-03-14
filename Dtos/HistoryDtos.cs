namespace RS.Fahrzeugsystem.Api.Dtos;

public sealed record CreateHistoryRequest(
    string EventType,
    string Title,
    string? Description,
    DateTime? EventDateUtc,
    long KmValue);

public sealed record UpdateHistoryRequest(
    string EventType,
    string Title,
    string? Description,
    DateTime? EventDateUtc,
    long KmValue);
