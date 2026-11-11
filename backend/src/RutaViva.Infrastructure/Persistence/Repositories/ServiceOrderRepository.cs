using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Infrastructure.Persistence.Models;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrderRepository(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IServiceOrderRepository
{
    public async Task<ServiceOrder> FindServiceOrderByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var orderData = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<ServiceOrderPersistenceModel>(
            Command(SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.GetById), new { id }, cancellationToken));
        if (orderData is null)
            throw new MissingException("Orden no encontrada.");
        return Map(orderData);
    }

    public async Task<bool> HasOpenOrderForRoutineAsync(
        Guid vehicleId,
        Guid routineId,
        CancellationToken cancellationToken)
    {
        var count = await UnitOfWork.Connection.ExecuteScalarAsync<int>(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.FindOpen),
            new { vehicleId, routineId },
            cancellationToken));
        return count > 0;
    }

    public async Task InsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken)
    {
        try
        {
            await UnitOfWork.Connection.ExecuteAsync(Command(
                SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.Insert),
                Params(order, null),
                cancellationToken));
        }
        catch (SqlException ex) when (SqlUniqueness.IsViolation(ex, "UX_ServiceOrders_OpenRoutine"))
        {
            throw new ConflictException("Ya existe una orden abierta para este vehículo y rutina.");
        }
    }

    public async Task SaveServiceOrderAsync(
        ServiceOrder order,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var affected = await UnitOfWork.Connection.ExecuteAsync(Command(
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

    private static ServiceOrder Map(ServiceOrderPersistenceModel orderData) =>
        ServiceOrder.Reconstitute(
            orderData.ServiceOrderId,
            orderData.Version,
            orderData.VehicleId,
            orderData.MaintenanceRoutineId,
            orderData.MaintenanceCycleId,
            orderData.AssignedWorkshop,
            Enum.Parse<OrderState>(orderData.Status),
            orderData.QuoteAmountCop,
            orderData.ActualServiceOdometerKilometers,
            orderData.ActualServiceDate,
            orderData.CreatedAt);
}
