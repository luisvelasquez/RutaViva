namespace RutaViva.Api.Requests.MaintenanceRoutines;

public sealed record CreateMaintenanceRoutineRequest(
    string Name,
    decimal? IntervalKm,
    int? IntervalDays,
    decimal LastServiceKm,
    DateOnly LastServiceDate);
