using RutaViva.Application.Maintenance;
using RutaViva.Domain;

namespace RutaViva.Application.Vehicles;

public sealed class RegisterOdometerReadingHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator)
{
    public async Task<object> HandleAsync(Guid vehicleId, RegisterReadingCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken);
        var originalVersion = vehicle.Version;
        var accepted = vehicle.RegisterReading(new Kilometrage(command.Kilometres));

        if (accepted)
        {
            await vehicles.UpdateAsync(vehicle, originalVersion, cancellationToken);
            foreach (var routine in await routines.GetByVehicleAsync(vehicleId, cancellationToken))
            {
                var routineVersion = routine.Version;
                await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
                if (routine.Version != routineVersion)
                    await routines.UpdateAsync(routine, routineVersion, cancellationToken);
            }
        }

        return new { accepted, vehicle = vehicle.ToDto() };
    }
}
