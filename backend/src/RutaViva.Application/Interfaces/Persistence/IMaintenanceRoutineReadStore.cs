using RutaViva.Application.MaintenanceRoutines.Dtos;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IMaintenanceRoutineReadStore
{
    Task<IReadOnlyList<MaintenanceRoutineDto>> ListRoutinesByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken);
}
