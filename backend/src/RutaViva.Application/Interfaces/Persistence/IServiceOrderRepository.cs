using RutaViva.Domain.Aggregates.ServiceOrders;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IServiceOrderRepository
{
    Task<ServiceOrder> FindServiceOrderByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasOpenOrderForRoutineAsync(Guid vehicleId, Guid routineId, CancellationToken cancellationToken);
    Task InsertServiceOrderAsync(ServiceOrder order, CancellationToken cancellationToken);
    Task SaveServiceOrderAsync(ServiceOrder order, long expectedVersion, CancellationToken cancellationToken);
}
