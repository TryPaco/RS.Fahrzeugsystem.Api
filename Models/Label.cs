namespace RS.Fahrzeugsystem.Api.Models;

public enum LabelStatus
{
    Free = 0,
    Assigned = 1,
    Disabled = 2
}

public enum LabelType
{
    Qr = 0,
    Barcode = 1
}

public sealed class Label : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int CodeNumber { get; set; }
    public LabelType Type { get; set; }
    public LabelStatus Status { get; set; } = LabelStatus.Free;

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public string? PositionOnVehicle { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public string? Notes { get; set; }
}
