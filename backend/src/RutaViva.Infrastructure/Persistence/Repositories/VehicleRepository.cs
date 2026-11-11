using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Infrastructure.Persistence.Rows;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IVehicleRepository
{
    public async Task<Vehicle> FindVehicleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<VehicleRow>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.GetById), new { id }, cancellationToken));
        if (row is null)
            throw new MissingException("Vehículo no encontrado.");
        return Vehicle.Reconstitute(row.Id, row.Version, row.Plate, row.Workshop, row.Odometer);
    }

    public async Task<IReadOnlyList<Guid>> ListVehicleIdsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var ids = await UnitOfWork.Connection.QueryAsync<Guid>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.ListIds), new { skip, take }, cancellationToken));
        return ids.ToList();
    }

    public Task InsertVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        UnitOfWork.Connection.ExecuteAsync(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.Insert),
            new
            {
                vehicle.Id,
                vehicle.Version,
                vehicle.Plate,
                vehicle.Workshop,
                Odometer = vehicle.Odometer.Value
            },
            cancellationToken));

    public async Task SaveVehicleAsync(Vehicle vehicle, long expectedVersion, CancellationToken cancellationToken)
    {
        var affected = await UnitOfWork.Connection.ExecuteAsync(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.Update),
            new
            {
                vehicle.Id,
                vehicle.Version,
                vehicle.Plate,
                vehicle.Workshop,
                Odometer = vehicle.Odometer.Value,
                ExpectedVersion = expectedVersion
            },
            cancellationToken));
        if (affected != 1)
            throw new ConflictException("La información cambió. Actualice y vuelva a intentar.");
    }
}
