using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.MaintenanceOverview.Dtos;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Models;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceOverviewReader(SqlConnectionFactory factory, IClock clock)
    : IMaintenanceOverviewReader
{
    public async Task<MaintenanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Dashboard.Load),
            cancellationToken: cancellationToken));

        var vehicles = (await result.ReadAsync<VehiclePersistenceModel>()).Select(MapVehicle).ToList();
        var routines = (await result.ReadAsync<MaintenanceRoutinePersistenceModel>()).Select(MapRoutine).ToList();
        var orders = (await result.ReadAsync<ServiceOrderPersistenceModel>()).Select(MapOrder).ToList();
        var events = (await result.ReadAsync<OutboxMessagePersistenceModel>()).Select(MapEvent).ToList();

        return new MaintenanceOverviewDto(vehicles, routines, orders, events, clock.Today);
    }

    internal static VehicleDto MapVehicle(VehiclePersistenceModel vehicleData) =>
        new(vehicleData.VehicleId, vehicleData.LicensePlate, vehicleData.AssignedWorkshop,
            vehicleData.OdometerKilometers, vehicleData.Version);

    internal static MaintenanceRoutineDto MapRoutine(MaintenanceRoutinePersistenceModel routineData) =>
        new(
            routineData.MaintenanceRoutineId,
            routineData.VehicleId,
            routineData.Name,
            routineData.IntervalKilometers,
            routineData.IntervalDays,
            routineData.LastServiceOdometerKilometers,
            routineData.LastServiceDate,
            routineData.MaintenanceCycleId,
            routineData.Status,
            routineData.DueSoonNoticeRaised,
            routineData.IsCycleCancelled,
            routineData.Version);

    internal static ServiceOrderDto MapOrder(ServiceOrderPersistenceModel orderData) =>
        new(
            orderData.ServiceOrderId,
            orderData.VehicleId,
            orderData.MaintenanceRoutineId,
            orderData.AssignedWorkshop,
            orderData.Status,
            orderData.QuoteAmountCop,
            orderData.ActualServiceOdometerKilometers,
            orderData.ActualServiceDate,
            orderData.Version,
            orderData.CreatedAt);

    internal static EventDto MapEvent(OutboxMessagePersistenceModel messageData) =>
        new(
            messageData.OutboxMessageId,
            messageData.EventType,
            messageData.AggregateId,
            messageData.MaintenanceCycleId,
            messageData.Payload,
            messageData.OccurredAt,
            messageData.PublishedAt);
}
