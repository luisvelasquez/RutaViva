using Dapper;
using RutaViva.Application;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(SqlUnitOfWork unitOfWork) : IVehicleRepository
{
    private CommandDefinition Cmd(string sql, object? args, CancellationToken ct) =>
        new(sql, args, unitOfWork.Transaction, cancellationToken: ct);

    public async Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<VehicleRow>(
            Cmd(SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.GetById), new { id }, cancellationToken));
        if (row is null)
            throw new MissingException("Vehículo no encontrado.");
        return Vehicle.Reconstitute(row.Id, row.Version, row.Plate, row.Workshop, row.Odometer);
    }

    public async Task<IReadOnlyList<Guid>> ListIdsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var ids = await unitOfWork.Connection.QueryAsync<Guid>(
            Cmd(SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.ListIds), new { skip, take }, cancellationToken));
        return ids.ToList();
    }

    public async Task InsertAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await unitOfWork.Connection.ExecuteAsync(Cmd(
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
    }

    public async Task UpdateAsync(Vehicle vehicle, long expectedVersion, CancellationToken cancellationToken)
    {
        var affected = await unitOfWork.Connection.ExecuteAsync(Cmd(
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
