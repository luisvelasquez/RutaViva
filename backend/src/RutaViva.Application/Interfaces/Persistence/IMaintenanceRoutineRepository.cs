using RutaViva.Domain.Aggregates.MaintenanceRoutines;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IMaintenanceRoutineRepository
{
    Task<MaintenanceRoutine> FindRoutineByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceRoutine>> FindRoutinesByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task InsertRoutineAsync(MaintenanceRoutine routine, CancellationToken cancellationToken);
    Task SaveRoutineAsync(MaintenanceRoutine routine, long expectedVersion, CancellationToken cancellationToken);
}
