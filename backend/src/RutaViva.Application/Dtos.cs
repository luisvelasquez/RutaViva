namespace RutaViva.Application;

public sealed record VehicleDto(Guid Id, string Plate, string Workshop, decimal Odometer, long Version);

public sealed record RoutineDto(
    Guid Id,
    Guid VehicleId,
    string Name,
    decimal? IntervalKm,
    int? IntervalDays,
    decimal LastServiceKm,
    DateOnly LastServiceDate,
    Guid CycleId,
    string Status,
    bool NoticeRaised,
    bool CycleCancelled,
    long Version);

public sealed record OrderDto(
    Guid Id,
    Guid VehicleId,
    Guid RoutineId,
    string Workshop,
    string State,
    decimal? QuoteAmount,
    decimal? ActualKm,
    DateOnly? ActualDate,
    long Version,
    DateTimeOffset CreatedAt);

public sealed record EventDto(
    Guid Id,
    string Type,
    Guid AggregateId,
    Guid CycleId,
    string Message,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PublishedAt);

public sealed record DashboardDto(
    List<VehicleDto> Vehicles,
    List<RoutineDto> Routines,
    List<OrderDto> Orders,
    List<EventDto> Events,
    DateOnly Today);

public sealed record CreateVehicleCommand(string Plate, string Workshop, decimal Odometer);
public sealed record CreateRoutineCommand(string Name, decimal? IntervalKm, int? IntervalDays, decimal LastServiceKm, DateOnly LastServiceDate);
public sealed record RegisterReadingCommand(decimal Kilometres);
public sealed record OrderActionCommand(long ExpectedVersion, decimal? Amount = null, decimal? Kilometres = null, DateOnly? Date = null);
