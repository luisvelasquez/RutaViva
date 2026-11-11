using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Infrastructure.Persistence.Rows;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRoutineRepository(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IMaintenanceRoutineRepository
{
    public async Task<MaintenanceRoutine> FindRoutineByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<MaintenanceRoutineRow>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetById), new { id }, cancellationToken));
        if (row is null)
            throw new MissingException("Rutina no encontrada.");
        return Map(row);
    }

    public async Task<IReadOnlyList<MaintenanceRoutine>> FindRoutinesByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var rows = await UnitOfWork.Connection.QueryAsync<MaintenanceRoutineRow>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetByVehicle), new { vehicleId }, cancellationToken));
        return rows.Select(Map).ToList();
    }

    public Task InsertRoutineAsync(MaintenanceRoutine routine, CancellationToken cancellationToken) =>
        UnitOfWork.Connection.ExecuteAsync(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.Insert),
            Params(routine, null),
            cancellationToken));

    public async Task SaveRoutineAsync(
        MaintenanceRoutine routine,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var affected = await UnitOfWork.Connection.ExecuteAsync(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.Update),
            Params(routine, expectedVersion),
            cancellationToken));
        if (affected != 1)
            throw new ConflictException("La información cambió. Actualice y vuelva a intentar.");
    }

    private static object Params(MaintenanceRoutine routine, long? expectedVersion) => new
    {
        routine.Id,
        routine.Version,
        routine.VehicleId,
        routine.Name,
        IntervalKm = routine.Interval.IntervalKilometres?.Value,
        IntervalDays = routine.Interval.Days,
        LastServiceKm = routine.LastServiceKm.Value,
        routine.LastServiceDate,
        routine.CycleId,
        routine.NoticeRaised,
        routine.CycleCancelled,
        Status = routine.Status.ToString(),
        ExpectedVersion = expectedVersion
    };

    private static MaintenanceRoutine Map(MaintenanceRoutineRow row) =>
        MaintenanceRoutine.Reconstitute(
            row.Id, row.Version, row.VehicleId, row.Name, row.IntervalKm, row.IntervalDays,
            row.LastServiceKm, row.LastServiceDate, row.CycleId, row.NoticeRaised, row.CycleCancelled,
            Enum.Parse<MaintenanceStatus>(row.Status));
}
