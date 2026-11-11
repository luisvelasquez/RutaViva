using RutaViva.Application.ServiceOrders.Dtos;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IServiceOrderReadStore
{
    Task<ServiceOrderDto> GetServiceOrderByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceOrderDto>> ListServiceOrdersAsync(
        Guid? vehicleId,
        string? status,
        CancellationToken cancellationToken);
}
