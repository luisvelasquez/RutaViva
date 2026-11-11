using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Infrastructure.Persistence.Models;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRoutineRepository(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IMaintenanceRoutineRepository
{
    public async Task<MaintenanceRoutine> FindRoutineByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var routineData = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<MaintenanceRoutinePersistenceModel>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetById), new { id }, cancellationToken));
        if (routineData is null)
            throw new MissingException("Rutina no encontrada.");
        return Map(routineData);
    }

    public async Task<IReadOnlyList<MaintenanceRoutine>> FindRoutinesByVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var routinesData = await UnitOfWork.Connection.QueryAsync<MaintenanceRoutinePersistenceModel>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.MaintenanceRoutines.GetByVehicle), new { vehicleId }, cancellationToken));
        return routinesData.Select(Map).ToList();
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

    private static MaintenanceRoutine Map(MaintenanceRoutinePersistenceModel routineData) =>
        MaintenanceRoutine.Reconstitute(
            routineData.MaintenanceRoutineId,
            routineData.Version,
            routineData.VehicleId,
            routineData.Name,
            routineData.IntervalKilometers,
            routineData.IntervalDays,
            routineData.LastServiceOdometerKilometers,
            routineData.LastServiceDate,
            routineData.MaintenanceCycleId,
            routineData.DueSoonNoticeRaised,
            routineData.IsCycleCancelled,
            Enum.Parse<MaintenanceStatus>(routineData.Status));
}
