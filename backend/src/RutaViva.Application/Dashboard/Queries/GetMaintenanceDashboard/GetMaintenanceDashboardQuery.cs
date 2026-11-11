using RutaViva.Application.Dashboard.Models;
using RutaViva.Application.Messaging;

namespace RutaViva.Application.Dashboard.Queries.GetMaintenanceDashboard;

public sealed record GetMaintenanceDashboardQuery : IQuery<MaintenanceDashboardDto>;
