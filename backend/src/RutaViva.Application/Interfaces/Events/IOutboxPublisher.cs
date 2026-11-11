namespace RutaViva.Application.Interfaces.Events;

/// <summary>
/// Publica mensajes pendientes del Outbox (al menos una vez). Distinto del aviso único por ciclo.
/// </summary>
public interface IOutboxPublisher
{
    Task<int> PublishPendingAsync(int take, CancellationToken cancellationToken);
}
