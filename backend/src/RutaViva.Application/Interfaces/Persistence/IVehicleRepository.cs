using RutaViva.Domain.Aggregates.Vehicles;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IVehicleRepository
{
    Task<Vehicle> FindVehicleByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListVehicleIdsAsync(int skip, int take, CancellationToken cancellationToken);
    Task InsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task SaveVehicleAsync(Vehicle vehicle, long expectedVersion, CancellationToken cancellationToken);
}
