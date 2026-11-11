using MediatR;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.MaintenanceRoutines.Coordination;

namespace RutaViva.Application.MaintenanceRoutines.Commands.EvaluateDueMaintenance;

public sealed class EvaluateDueMaintenanceCommandHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator,
    IApplicationLock applicationLock,
    IClock clock)
    : IRequestHandler<EvaluateDueMaintenanceCommand, EvaluateDueMaintenanceResult>
{
    public const int PageSize = 50;

    public async Task<EvaluateDueMaintenanceResult> Handle(
        EvaluateDueMaintenanceCommand command,
        CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var skip = 0;

        while (true)
        {
            var page = await vehicles.ListVehicleIdsAsync(skip, PageSize, cancellationToken);
            if (page.Count == 0)
                break;

            foreach (var vehicleId in page.OrderBy(id => id))
            {
                await applicationLock.AcquireResourceLockAsync($"vehicle:{vehicleId:N}", cancellationToken);
                var vehicle = await vehicles.FindVehicleByIdAsync(vehicleId, cancellationToken);
                foreach (var routine in await routines.FindRoutinesByVehicleAsync(vehicleId, cancellationToken))
                {
                    var original = routine.Version;
                    await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
                    if (routine.Version != original)
                        await routines.SaveRoutineAsync(routine, original, cancellationToken);
                    evaluated++;
                }
            }

            if (page.Count < PageSize)
                break;
            skip += PageSize;
        }

        return new EvaluateDueMaintenanceResult(evaluated, clock.Today);
    }
}
