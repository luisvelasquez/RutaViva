using Dapper;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Domain;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Models;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class ServiceOrderReadStore(SqlConnectionFactory factory) : IServiceOrderReadStore
{
    public async Task<ServiceOrderDto> GetServiceOrderByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        var orderData = await connection.QuerySingleOrDefaultAsync<ServiceOrderPersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.GetById),
            new { id = orderId },
            cancellationToken: cancellationToken));
        if (orderData is null)
            throw new MissingException("Orden no encontrada.");
        return MaintenanceOverviewReader.MapOrder(orderData);
    }

    public async Task<IReadOnlyList<ServiceOrderDto>> ListServiceOrdersAsync(
        Guid? vehicleId,
        string? status,
        CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        var ordersData = await connection.QueryAsync<ServiceOrderPersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.ServiceOrders.List),
            new { vehicleId, status },
            cancellationToken: cancellationToken));
        return ordersData.Select(MaintenanceOverviewReader.MapOrder).ToList();
    }
}
