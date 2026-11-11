using RutaViva.Application;
using RutaViva.Application.Maintenance;
using RutaViva.Application.Orders;
using RutaViva.Application.Routines;
using RutaViva.Application.Vehicles;
using RutaViva.Infrastructure;
using RutaViva.Infrastructure.Persistence;
using RutaViva.Infrastructure.Persistence.Repositories;

namespace RutaViva.Api;

public static class Composition
{
    public static IServiceCollection AddRutaViva(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Database.");

        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<SqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SqlUnitOfWork>());
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IMaintenanceRoutineRepository, MaintenanceRoutineRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IIdempotentExecutor, IdempotentExecutor>();
        services.AddScoped<IReadModel, ReadModel>();

        services.AddScoped<DueMaintenanceCoordinator>();
        services.AddScoped<CreateVehicleHandler>();
        services.AddScoped<RegisterOdometerReadingHandler>();
        services.AddScoped<CreateMaintenanceRoutineHandler>();
        services.AddScoped<EvaluateDueMaintenanceHandler>();
        services.AddScoped<QuoteServiceOrderHandler>();
        services.AddScoped<ApproveServiceOrderHandler>();
        services.AddScoped<RejectServiceOrderHandler>();
        services.AddScoped<CancelServiceOrderHandler>();
        services.AddScoped<StartServiceOrderHandler>();
        services.AddScoped<CompleteServiceOrderHandler>();

        return services;
    }
}
