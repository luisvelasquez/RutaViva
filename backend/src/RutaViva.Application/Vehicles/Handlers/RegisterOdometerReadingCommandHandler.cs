using MediatR;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Domain.ValueObjects;
using RutaViva.Application.Vehicles.Mappings;
using RutaViva.Application.Vehicles.Commands;

namespace RutaViva.Application.Vehicles.Handlers;

public sealed class RegisterOdometerReadingCommandHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator,
    IApplicationLock applicationLock)
    : IRequestHandler<RegisterOdometerReadingCommand, RegisterOdometerReadingResult>
{
    public async Task<RegisterOdometerReadingResult> Handle(
        RegisterOdometerReadingCommand command,
        CancellationToken cancellationToken)
    {
        await applicationLock.AcquireResourceLockAsync($"vehicle:{command.VehicleId:N}", cancellationToken);

        var vehicle = await vehicles.FindVehicleByIdAsync(command.VehicleId, cancellationToken);
        var originalVersion = vehicle.Version;
        var accepted = vehicle.RecordOdometerReading(new Kilometres(command.Kilometres));

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
