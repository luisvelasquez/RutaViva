using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Vehicles.Models;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Application.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleCommandHandler(IVehicleRepository vehicles)
    : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle(command.Plate, command.Workshop, new Kilometrage(command.Odometer));
        await vehicles.InsertVehicleAsync(vehicle, cancellationToken);
        return vehicle.ToDto();
    }
}
