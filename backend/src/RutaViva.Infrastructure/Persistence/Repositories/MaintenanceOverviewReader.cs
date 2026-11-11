using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Rows;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.MaintenanceOverview.Dtos;

namespace RutaViva.Infrastructure.Persistence.Repositories;

/// <summary>Consulta de lectura con conexión propia (fuera de la UoW de escritura).</summary>
public sealed class MaintenanceOverviewReader(SqlConnectionFactory factory, IClock clock)
    : IMaintenanceOverviewReader
{
    public async Task<MaintenanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Dashboard.Load),
            cancellationToken: cancellationToken));

        var vehicles = (await result.ReadAsync<VehicleRow>()).Select(v =>
            new VehicleDto(v.Id, v.Plate, v.Workshop, v.Odometer, v.Version)).ToList();

        var routines = (await result.ReadAsync<MaintenanceRoutineRow>()).Select(r =>
            new MaintenanceRoutineDto(
                r.Id, r.VehicleId, r.Name, r.IntervalKm, r.IntervalDays,
                r.LastServiceKm, r.LastServiceDate, r.CycleId, r.Status,
                r.NoticeRaised, r.CycleCancelled, r.Version)).ToList();

        var orders = (await result.ReadAsync<ServiceOrderRow>()).Select(o =>
            new ServiceOrderDto(
                o.Id, o.VehicleId, o.RoutineId, o.Workshop, o.State,
                o.QuoteAmount, o.ActualKm, o.ActualDate, o.Version, o.CreatedAt)).ToList();

        var events = (await result.ReadAsync<OutboxMessageRow>()).Select(e =>
            new EventDto(e.Id, e.Type, e.AggregateId, e.CycleId, e.Message, e.OccurredAt, e.PublishedAt)).ToList();

        return new MaintenanceOverviewDto(vehicles, routines, orders, events, clock.Today);
    }
}
