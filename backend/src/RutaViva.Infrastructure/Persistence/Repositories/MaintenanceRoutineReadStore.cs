using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Models;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRoutineReadStore(SqlConnectionFactory factory) : IMaintenanceRoutineReadStore
{
    public async Task<IReadOnlyList<MaintenanceRoutineDto>> ListRoutinesByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        // Ensure the vehicle exists before listing (404 for unknown vehicleId).
        var vehicleExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM dbo.Vehicles WHERE VehicleId = @vehicleId",
            new { vehicleId },
            cancellationToken: cancellationToken));
        if (vehicleExists == 0)
            throw new MissingException("Vehículo no encontrado.");

        var routinesData = await connection.QueryAsync<MaintenanceRoutinePersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetByVehicle),
            new { vehicleId },
            cancellationToken: cancellationToken));
        return routinesData.Select(MaintenanceOverviewReader.MapRoutine).ToList();
    }
}
