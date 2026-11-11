using RutaViva.Application.Messaging.Interfaces;
using RutaViva.Application.ServiceOrders.Dtos;

namespace RutaViva.Application.ServiceOrders.Queries;

public sealed record GetServiceOrderByIdQuery(Guid OrderId) : IQuery<ServiceOrderDto>;

public sealed record ListServiceOrdersQuery(Guid? VehicleId, string? Status) : IQuery<IReadOnlyList<ServiceOrderDto>>;
