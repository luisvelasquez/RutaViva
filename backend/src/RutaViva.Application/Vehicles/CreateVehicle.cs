using RutaViva.Domain;

namespace RutaViva.Application.Vehicles;

public sealed class CreateVehicleHandler(IVehicleRepository vehicles)
{
    public async Task<VehicleDto> HandleAsync(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle(command.Plate, command.Workshop, new Kilometrage(command.Odometer));
        await vehicles.InsertAsync(vehicle, cancellationToken);
        return vehicle.ToDto();
    }
}
