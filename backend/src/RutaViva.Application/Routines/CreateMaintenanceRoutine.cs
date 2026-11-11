using RutaViva.Application.Maintenance;
using RutaViva.Domain;

namespace RutaViva.Application.Routines;

public sealed class CreateMaintenanceRoutineHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator,
    IClock clock)
{
    public async Task<Guid> HandleAsync(Guid vehicleId, CreateRoutineCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken);
        if (command.LastServiceKm > vehicle.Odometer.Value || command.LastServiceDate > clock.Today)
            throw new BusinessException("El último servicio no puede superar el odómetro ni tener fecha futura.");

        var routine = new MaintenanceRoutine(
            vehicleId,
            command.Name,
            new MaintenanceInterval(command.IntervalKm, command.IntervalDays),
            new Kilometrage(command.LastServiceKm),
            command.LastServiceDate);

        await routines.InsertAsync(routine, cancellationToken);

        var originalVersion = routine.Version;
        await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
        if (routine.Version != originalVersion)
            await routines.UpdateAsync(routine, originalVersion, cancellationToken);

        return routine.Id;
    }
}
