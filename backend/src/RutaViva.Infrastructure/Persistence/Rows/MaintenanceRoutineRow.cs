namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class MaintenanceRoutineRow
{
    public Guid Id { get; init; }
    public long Version { get; init; }
    public Guid VehicleId { get; init; }
    public string Name { get; init; } = "";
    public decimal? IntervalKm { get; init; }
    public int? IntervalDays { get; init; }
    public decimal LastServiceKm { get; init; }
    public DateOnly LastServiceDate { get; init; }
    public Guid CycleId { get; init; }
    public bool NoticeRaised { get; init; }
    public bool CycleCancelled { get; init; }
    public string Status { get; init; } = "";
}
