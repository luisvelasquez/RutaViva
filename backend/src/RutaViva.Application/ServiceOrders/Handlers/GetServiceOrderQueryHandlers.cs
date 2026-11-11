using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Queries;

namespace RutaViva.Application.ServiceOrders.Handlers;

public sealed class GetServiceOrderByIdQueryHandler(IServiceOrderReadStore readStore)
    : IRequestHandler<GetServiceOrderByIdQuery, ServiceOrderDto>
{
    public Task<ServiceOrderDto> Handle(GetServiceOrderByIdQuery request, CancellationToken cancellationToken) =>
        readStore.GetServiceOrderByIdAsync(request.OrderId, cancellationToken);
}

public sealed class ListServiceOrdersQueryHandler(IServiceOrderReadStore readStore)
    : IRequestHandler<ListServiceOrdersQuery, IReadOnlyList<ServiceOrderDto>>
{
    public Task<IReadOnlyList<ServiceOrderDto>> Handle(
        ListServiceOrdersQuery request,
        CancellationToken cancellationToken) =>
        readStore.ListServiceOrdersAsync(request.VehicleId, request.Status, cancellationToken);
}
