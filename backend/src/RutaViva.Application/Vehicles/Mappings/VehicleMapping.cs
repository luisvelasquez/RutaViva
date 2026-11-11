using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Application.Vehicles.Dtos;

namespace RutaViva.Application.Vehicles.Mappings;

public static class VehicleMapping
{
    public static VehicleDto ToDto(this Vehicle vehicle) =>
        new(vehicle.Id, vehicle.Plate, vehicle.Workshop, vehicle.Odometer.Value, vehicle.Version);
}
