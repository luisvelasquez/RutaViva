using RutaViva.Application.MaintenanceOverview.Dtos;
using RutaViva.Application.Messaging.Interfaces;

namespace RutaViva.Application.MaintenanceOverview.Queries;

public sealed record GetMaintenanceOverviewQuery : IQuery<MaintenanceOverviewDto>;
