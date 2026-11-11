using RutaViva.Application.Vehicles.Dtos;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IVehicleReadStore
{
    Task<VehicleDto> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken);
}
