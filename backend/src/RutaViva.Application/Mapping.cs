using RutaViva.Domain;

namespace RutaViva.Application;

public static class Mapping
{
    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(vehicle.Id, vehicle.Plate, vehicle.Workshop, vehicle.Odometer.Value, vehicle.Version);

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

    public static OrderDto ToDto(this ServiceOrder order) =>
        new(
            order.Id,
            order.VehicleId,
            order.RoutineId,
            order.Workshop,
            order.State.ToString(),
            order.QuotedAmount?.Amount,
            order.ActualKm?.Value,
            order.ActualDate,
            order.Version,
            order.CreatedAt);
}
