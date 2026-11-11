using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.ValueObjects;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.Vehicles.Mappings;
using RutaViva.Application.Vehicles.Commands;

namespace RutaViva.Application.Vehicles.Handlers;

public sealed class CreateVehicleCommandHandler(IVehicleRepository vehicles)
    : IRequestHandler<CreateVehicleCommand, VehicleDto>
{
    public async Task<VehicleDto> Handle(CreateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle(command.Plate, command.Workshop, new Kilometres(command.Odometer));
        await vehicles.InsertVehicleAsync(vehicle, cancellationToken);
        return vehicle.ToDto();
    }
}
