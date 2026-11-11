namespace RutaViva.Application.MaintenanceRoutines.Models;

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
