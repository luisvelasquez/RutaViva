using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Application.MaintenanceRoutines.Dtos;

namespace RutaViva.Application.MaintenanceRoutines.Mappings;

public static class MaintenanceRoutineMapping
{
    public static MaintenanceRoutineDto ToDto(this MaintenanceRoutine routine) =>
        new(
            routine.Id,
            routine.VehicleId,
            routine.Name,
            routine.Interval.IntervalKilometres?.Value,
            routine.Interval.Days,
            routine.LastServiceKm.Value,
            routine.LastServiceDate,
            routine.CycleId,
            routine.Status.ToString(),
            routine.NoticeRaised,
            routine.CycleCancelled,
            routine.Version);
}
