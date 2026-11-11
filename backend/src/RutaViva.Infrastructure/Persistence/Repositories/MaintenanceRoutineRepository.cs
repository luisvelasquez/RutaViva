using Dapper;
using RutaViva.Application;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRoutineRepository(SqlUnitOfWork unitOfWork) : IMaintenanceRoutineRepository
{
    private CommandDefinition Cmd(string sql, object? args, CancellationToken ct) =>
        new(sql, args, unitOfWork.Transaction, cancellationToken: ct);

    public async Task<MaintenanceRoutine> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<RoutineRow>(
            Cmd(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetById), new { id }, cancellationToken));
        if (row is null)
            throw new MissingException("Rutina no encontrada.");
        return Map(row);
    }

    public async Task<IReadOnlyList<MaintenanceRoutine>> GetByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var rows = await unitOfWork.Connection.QueryAsync<RoutineRow>(
            Cmd(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetByVehicle), new { vehicleId }, cancellationToken));
        return rows.Select(Map).ToList();
    }

    public async Task InsertAsync(MaintenanceRoutine routine, CancellationToken cancellationToken)
    {
        await unitOfWork.Connection.ExecuteAsync(Cmd(
            SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.Insert),
            Params(routine, null),
            cancellationToken));
    }

    public async Task UpdateAsync(MaintenanceRoutine routine, long expectedVersion, CancellationToken cancellationToken)
    {
        var affected = await unitOfWork.Connection.ExecuteAsync(Cmd(
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
        IntervalKm = routine.Interval.Kilometres?.Value,
        IntervalDays = routine.Interval.Days,
        LastServiceKm = routine.LastServiceKm.Value,
        routine.LastServiceDate,
        routine.CycleId,
        routine.NoticeRaised,
        routine.CycleCancelled,
        Status = routine.Status.ToString(),
        ExpectedVersion = expectedVersion
    };

    private static MaintenanceRoutine Map(RoutineRow row) =>
        MaintenanceRoutine.Reconstitute(
            row.Id,
            row.Version,
            row.VehicleId,
            row.Name,
            row.IntervalKm,
            row.IntervalDays,
            row.LastServiceKm,
            row.LastServiceDate,
            row.CycleId,
            row.NoticeRaised,
            row.CycleCancelled,
            Enum.Parse<MaintenanceStatus>(row.Status));
}
