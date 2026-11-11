namespace RutaViva.Infrastructure.Persistence.Models;

public sealed class ServiceOrderPersistenceModel
{
    public Guid ServiceOrderId { get; init; }
    public long Version { get; init; }
    public Guid VehicleId { get; init; }
    public Guid MaintenanceRoutineId { get; init; }
    public Guid MaintenanceCycleId { get; init; }
    public string AssignedWorkshop { get; init; } = "";
    public string Status { get; init; } = "";
    public decimal? QuoteAmountCop { get; init; }
    public decimal? ActualServiceOdometerKilometers { get; init; }
    public DateOnly? ActualServiceDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
