using RutaViva.Domain.Aggregates;
using RutaViva.Domain.Events;

namespace RutaViva.Application.Interfaces.Events;

public interface IOutboxWriter
{
    Task AppendAsync(DomainEvent domainEvent, DateTimeOffset occurredAt, CancellationToken cancellationToken);

    async Task FlushAsync(AggregateRoot aggregate, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in aggregate.DequeueDomainEvents())
            await AppendAsync(domainEvent, occurredAt, cancellationToken);
    }
}
