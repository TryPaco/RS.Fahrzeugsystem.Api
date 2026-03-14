namespace RS.Fahrzeugsystem.Api.Models;

public sealed class PartCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<VehiclePart> Parts { get; set; } = new List<VehiclePart>();
}
