using MediatR;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.MaintenanceRoutines.Commands;
using RutaViva.Application.Messaging.Idempotency;

namespace RutaViva.Api.Workers;

/// <summary>
/// Revisión diaria por tiempo. Independiente del publicador Outbox para reintentos separados.
/// Una sola transacción MediatR/idempotencia pagina vehículos; el bloqueo es por vehículo dentro del handler.
/// </summary>
public sealed class DailyMaintenanceWorker(IServiceScopeFactory scopes, ILogger<DailyMaintenanceWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var clock = scope.ServiceProvider.GetRequiredService<IClock>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var command = new EvaluateDueMaintenanceCommand(
                    new IdempotencyContext(
                        "system/daily",
                        clock.Today.ToString("yyyy-MM-dd"),
                        "daily-v1",
                        []));
                await mediator.Send(command, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló la revisión diaria; se reintentará.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
