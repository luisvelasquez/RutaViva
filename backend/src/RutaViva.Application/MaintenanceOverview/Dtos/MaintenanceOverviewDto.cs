using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.Vehicles.Dtos;

namespace RutaViva.Application.MaintenanceOverview.Dtos;

public sealed record EventDto(
    Guid Id,
    string Type,
    Guid AggregateId,
    Guid CycleId,
    string Message,
    DateTimeOffset OccurredAt,
    DateTimeOffset? PublishedAt);

public sealed record MaintenanceOverviewDto(
    List<VehicleDto> Vehicles,
    List<MaintenanceRoutineDto> Routines,
    List<ServiceOrderDto> Orders,
    List<EventDto> Events,
    DateOnly Today);
