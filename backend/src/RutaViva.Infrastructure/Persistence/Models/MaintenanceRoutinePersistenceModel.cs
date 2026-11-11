namespace RutaViva.Infrastructure.Persistence.Models;

public sealed class MaintenanceRoutinePersistenceModel
{
    public Guid MaintenanceRoutineId { get; init; }
    public long Version { get; init; }
    public Guid VehicleId { get; init; }
    public string Name { get; init; } = "";
    public decimal? IntervalKilometers { get; init; }
    public int? IntervalDays { get; init; }
    public decimal LastServiceOdometerKilometers { get; init; }
    public DateOnly LastServiceDate { get; init; }
    public Guid MaintenanceCycleId { get; init; }
    public bool DueSoonNoticeRaised { get; init; }
    public bool IsCycleCancelled { get; init; }
    public string Status { get; init; } = "";
}
