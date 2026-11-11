using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.Dashboard.Models;
using RutaViva.Application.MaintenanceRoutines.Models;
using RutaViva.Application.ServiceOrders.Models;
using RutaViva.Application.Vehicles.Models;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Repositories;

/// <summary>Consulta de lectura con conexión propia (fuera de la UoW de escritura).</summary>
public sealed class MaintenanceDashboardReadStore(SqlConnectionFactory factory, IClock clock)
    : IMaintenanceDashboardReadStore
{
    public async Task<MaintenanceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Dashboard.Load),
            cancellationToken: cancellationToken));

        var vehicles = (await result.ReadAsync<VehicleRow>()).Select(v =>
            new VehicleDto(v.Id, v.Plate, v.Workshop, v.Odometer, v.Version)).ToList();

        var routines = (await result.ReadAsync<RoutineRow>()).Select(r =>
            new RoutineDto(
                r.Id, r.VehicleId, r.Name, r.IntervalKm, r.IntervalDays,
                r.LastServiceKm, r.LastServiceDate, r.CycleId, r.Status,
                r.NoticeRaised, r.CycleCancelled, r.Version)).ToList();

        var orders = (await result.ReadAsync<OrderRow>()).Select(o =>
            new OrderDto(
                o.Id, o.VehicleId, o.RoutineId, o.Workshop, o.State,
                o.QuoteAmount, o.ActualKm, o.ActualDate, o.Version, o.CreatedAt)).ToList();

        var events = (await result.ReadAsync<OutboxRow>()).Select(e =>
            new EventDto(e.Id, e.Type, e.AggregateId, e.CycleId, e.Message, e.OccurredAt, e.PublishedAt)).ToList();

        return new MaintenanceDashboardDto(vehicles, routines, orders, events, clock.Today);
    }
}
