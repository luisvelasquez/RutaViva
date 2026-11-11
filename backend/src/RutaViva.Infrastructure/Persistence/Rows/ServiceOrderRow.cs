namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class ServiceOrderRow
{
    public Guid Id { get; init; }
    public long Version { get; init; }
    public Guid VehicleId { get; init; }
    public Guid RoutineId { get; init; }
    public Guid CycleId { get; init; }
    public string Workshop { get; init; } = "";
    public string State { get; init; } = "";
    public decimal? QuoteAmount { get; init; }
    public decimal? ActualKm { get; init; }
    public DateOnly? ActualDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
