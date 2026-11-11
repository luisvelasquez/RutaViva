using RutaViva.Domain.Aggregates.Vehicles;

namespace RutaViva.Application.Vehicles.Models;

public static class VehicleMapping
{
    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(vehicle.Id, vehicle.Plate, vehicle.Workshop, vehicle.Odometer.Value, vehicle.Version);
}
