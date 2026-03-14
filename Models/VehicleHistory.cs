namespace RS.Fahrzeugsystem.Api.Models;

public sealed class VehicleHistory : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDateUtc { get; set; } = DateTime.UtcNow;
    public bool KmRequired { get; set; } = true;
    public long KmValue { get; set; }
    public Guid CreatedByUserId { get; set; }
}
