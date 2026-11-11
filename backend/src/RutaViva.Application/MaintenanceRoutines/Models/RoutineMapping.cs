using RutaViva.Domain.Aggregates.MaintenanceRoutines;

namespace RutaViva.Application.MaintenanceRoutines.Models;

public static class RoutineMapping
{
    public static RoutineDto ToDto(this MaintenanceRoutine routine) =>
        new(
            routine.Id,
            routine.VehicleId,
            routine.Name,
            routine.Interval.Kilometres?.Value,
            routine.Interval.Days,
            routine.LastServiceKm.Value,
            routine.LastServiceDate,
            routine.CycleId,
            routine.Status.ToString(),
            routine.NoticeRaised,
            routine.CycleCancelled,
            routine.Version);
}
