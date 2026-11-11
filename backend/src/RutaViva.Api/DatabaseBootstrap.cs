using RutaViva.Application;
using RutaViva.Application.Maintenance;
using RutaViva.Domain;
using RutaViva.Infrastructure;

namespace RutaViva.Api;

public static class DatabaseBootstrap
{
    public static async Task Initialize(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<SqlConnectionFactory>();
        await SqlDatabase.Initialize(factory);

        if (!configuration.GetValue("SeedDemo", true))
            return;

        var tx = scope.ServiceProvider.GetRequiredService<IIdempotentExecutor>();
        var vehicles = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var routines = scope.ServiceProvider.GetRequiredService<IMaintenanceRoutineRepository>();
        var evaluate = scope.ServiceProvider.GetRequiredService<EvaluateDueMaintenanceHandler>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        await tx.ExecuteAsync("system/seed", "demo-v1", "v1", [], async ct =>
        {
            var vehicle = new Vehicle("ABC123", "Taller Central", new Kilometrage(25_000));
            await vehicles.InsertAsync(vehicle, ct);

            var oil = new MaintenanceRoutine(vehicle.Id, "Cambio de aceite", new(10_000, 180), new(16_000), clock.Today.AddDays(-100));
            var brakes = new MaintenanceRoutine(vehicle.Id, "Revisión de frenos", new(20_000, null), new(5_000), clock.Today.AddDays(-200));
            var tyres = new MaintenanceRoutine(vehicle.Id, "Rotación de llantas", new(null, 90), new(23_000), clock.Today.AddDays(-20));
            await routines.InsertAsync(oil, ct);
            await routines.InsertAsync(brakes, ct);
            await routines.InsertAsync(tyres, ct);

            await evaluate.HandleAsync(ct);
            return "{}";
        }, CancellationToken.None);
    }
}
