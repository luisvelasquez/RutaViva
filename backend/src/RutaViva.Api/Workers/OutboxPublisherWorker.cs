using RutaViva.Application.Interfaces.Events;

namespace RutaViva.Api.Workers;

public sealed class OutboxPublisherWorker(IServiceScopeFactory scopes, ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
                await publisher.PublishPendingAsync(100, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló la publicación de Outbox; se reintentará.");
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
