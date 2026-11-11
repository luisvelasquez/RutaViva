namespace RutaViva.Application.Maintenance;

/// <summary>Revisión diaria acotada: páginas de vehículos, un vehículo a la vez.</summary>
public sealed class EvaluateDueMaintenanceHandler(
    IVehicleRepository vehicles,
    IMaintenanceRoutineRepository routines,
    DueMaintenanceCoordinator coordinator,
    IIdempotencyStore locks,
    IClock clock)
{
    public const int PageSize = 50;

    public async Task<object> HandleAsync(CancellationToken cancellationToken)
    {
        var evaluated = 0;
        var skip = 0;

        while (true)
        {
            var page = await vehicles.ListIdsAsync(skip, PageSize, cancellationToken);
            if (page.Count == 0)
                break;

            foreach (var vehicleId in page.OrderBy(id => id))
            {
                await locks.AcquireResourceLockAsync($"vehicle:{vehicleId:N}", cancellationToken);
                var vehicle = await vehicles.GetByIdAsync(vehicleId, cancellationToken);
                foreach (var routine in await routines.GetByVehicleAsync(vehicleId, cancellationToken))
                {
                    var original = routine.Version;
                    await coordinator.EvaluateRoutineAsync(vehicle, routine, cancellationToken);
                    if (routine.Version != original)
                        await routines.UpdateAsync(routine, original, cancellationToken);
                    evaluated++;
                }
            }

            if (page.Count < PageSize)
                break;
            skip += PageSize;
        }

        return new { evaluated, date = clock.Today };
    }
}
