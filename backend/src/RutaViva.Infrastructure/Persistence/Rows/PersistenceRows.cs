namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class VehicleRow
{
    public Guid Id { get; init; }
    public long Version { get; init; }
    public string Plate { get; init; } = "";
    public string Workshop { get; init; } = "";
    public decimal Odometer { get; init; }
}

public sealed class RoutineRow
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

public sealed class OrderRow
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

public sealed class OutboxRow
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public Guid AggregateId { get; init; }
    public Guid CycleId { get; init; }
    public string Message { get; init; } = "";
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}

public sealed class ReceiptRow
{
    public string Fingerprint { get; init; } = "";
    public string Response { get; init; } = "";
}
