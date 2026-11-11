namespace RutaViva.Infrastructure.Persistence.Models;

public sealed class VehiclePersistenceModel
{
    public Guid VehicleId { get; init; }
    public long Version { get; init; }
    public string LicensePlate { get; init; } = "";
    public string AssignedWorkshop { get; init; } = "";
    public decimal OdometerKilometers { get; init; }
}
