using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Models;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class VehicleReadStore(SqlConnectionFactory factory) : IVehicleReadStore
{
    public async Task<VehicleDto> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        var vehicleData = await connection.QuerySingleOrDefaultAsync<VehiclePersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.GetById),
            new { id = vehicleId },
            cancellationToken: cancellationToken));
        if (vehicleData is null)
            throw new MissingException("Vehículo no encontrado.");
        return MaintenanceOverviewReader.MapVehicle(vehicleData);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        var vehiclesData = await connection.QueryAsync<VehiclePersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.List),
            cancellationToken: cancellationToken));
        return vehiclesData.Select(MaintenanceOverviewReader.MapVehicle).ToList();
    }
}
