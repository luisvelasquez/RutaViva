using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Application.MaintenanceRoutines.Commands.CreateMaintenanceRoutine;

public sealed class CreateMaintenanceRoutineCommandHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator,
    IClock clock)
    : IRequestHandler<CreateMaintenanceRoutineCommand, CreateMaintenanceRoutineResult>
{
    public async Task<CreateMaintenanceRoutineResult> Handle(
        CreateMaintenanceRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.FindVehicleByIdAsync(command.VehicleId, cancellationToken);
        if (command.LastServiceKm > vehicle.Odometer.Value || command.LastServiceDate > clock.Today)
            throw new BusinessException("El último servicio no puede superar el odómetro ni tener fecha futura.");

        var routine = new MaintenanceRoutine(
            command.VehicleId,
            command.Name,
            new MaintenanceInterval(command.IntervalKm, command.IntervalDays),
            new Kilometrage(command.LastServiceKm),
            command.LastServiceDate);

        await routines.InsertRoutineAsync(routine, cancellationToken);

        var originalVersion = routine.Version;
        await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
        if (routine.Version != originalVersion)
            await routines.SaveRoutineAsync(routine, originalVersion, cancellationToken);

        return new CreateMaintenanceRoutineResult(routine.Id);
    }
}
