using RutaViva.Application.MaintenanceRoutines.Models;
using RutaViva.Application.ServiceOrders.Models;
using RutaViva.Application.Vehicles.Models;

namespace RutaViva.Application.Dashboard.Models;

public sealed record EventDto(
    Guid Id,
    string Type,
    Guid AggregateId,
    Guid CycleId,
    string Message,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PublishedAt);

public sealed record MaintenanceDashboardDto(
    List<VehicleDto> Vehicles,
    List<RoutineDto> Routines,
    List<OrderDto> Orders,
    List<EventDto> Events,
    DateOnly Today);
