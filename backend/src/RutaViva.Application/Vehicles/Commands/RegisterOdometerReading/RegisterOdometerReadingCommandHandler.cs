using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Application.Vehicles.Models;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Application.Vehicles.Commands.RegisterOdometerReading;

public sealed class RegisterOdometerReadingCommandHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator)
    : IRequestHandler<RegisterOdometerReadingCommand, RegisterOdometerReadingResult>
{
    public async Task<RegisterOdometerReadingResult> Handle(
        RegisterOdometerReadingCommand command,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.FindVehicleByIdAsync(command.VehicleId, cancellationToken);
        var originalVersion = vehicle.Version;
        var accepted = vehicle.RegisterOdometerReading(new Kilometrage(command.Kilometres));

        if (accepted)
        {
            await vehicles.SaveVehicleAsync(vehicle, originalVersion, cancellationToken);
            foreach (var routine in await routines.FindRoutinesByVehicleAsync(command.VehicleId, cancellationToken))
            {
                var routineVersion = routine.Version;
                await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
                if (routine.Version != routineVersion)
                    await routines.SaveRoutineAsync(routine, routineVersion, cancellationToken);
            }
        }

        return new RegisterOdometerReadingResult(accepted, vehicle.ToDto());
    }
}
