namespace RutaViva.Application.Interfaces.Events;

public interface IOutboxPublisher
{
    Task<int> PublishPendingAsync(int take, CancellationToken cancellationToken);
}
