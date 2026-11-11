using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrderRepository(SqlUnitOfWork unitOfWork) : IServiceOrderRepository
{
    private CommandDefinition Cmd(string sql, object? args, CancellationToken ct) =>
        new(sql, args, unitOfWork.Transaction, cancellationToken: ct);

    public async Task<ServiceOrder> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<OrderRow>(
            Cmd(SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.GetById), new { id }, cancellationToken));
        if (row is null)
            throw new MissingException("Orden no encontrada.");
        return Map(row);
    }

    public async Task<bool> HasOpenAsync(Guid vehicleId, Guid routineId, CancellationToken cancellationToken)
    {
        var count = await unitOfWork.Connection.ExecuteScalarAsync<int>(Cmd(
            SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.FindOpen),
            new { vehicleId, routineId },
            cancellationToken));
        return count > 0;
    }

    public async Task InsertAsync(ServiceOrder order, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.Connection.ExecuteAsync(Cmd(
                SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.Insert),
                Params(order, null),
                cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new ConflictException("Ya existe una orden abierta para este vehículo y rutina.");
        }
    }

    public async Task UpdateAsync(ServiceOrder order, long expectedVersion, CancellationToken cancellationToken)
    {
        var affected = await unitOfWork.Connection.ExecuteAsync(Cmd(
            SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.Update),
            Params(order, expectedVersion),
            cancellationToken));
        if (affected != 1)
            throw new ConflictException("La información cambió. Actualice y vuelva a intentar.");
    }

    private static object Params(ServiceOrder order, long? expectedVersion) => new
    {
        order.Id,
        order.Version,
        order.VehicleId,
        order.RoutineId,
        order.CycleId,
        order.Workshop,
        State = order.State.ToString(),
        QuoteAmount = order.QuotedAmount?.Amount,
        ActualKm = order.ActualKm?.Value,
        ActualDate = order.ActualDate,
        order.CreatedAt,
        ExpectedVersion = expectedVersion
    };

    private static ServiceOrder Map(OrderRow row) =>
        ServiceOrder.Reconstitute(
            row.Id,
            row.Version,
            row.VehicleId,
            row.RoutineId,
            row.CycleId,
            row.Workshop,
            Enum.Parse<OrderState>(row.State),
            row.QuoteAmount,
            row.ActualKm,
            row.ActualDate,
            row.CreatedAt);
}
