using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Domain.Events;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class OutboxWriter(SqlUnitOfWork unitOfWork) : SqlRepositoryBase(unitOfWork), IOutboxWriter
{
    public async Task AppendAsync(
        DomainEvent domainEvent,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        try
        {
            await UnitOfWork.Connection.ExecuteAsync(Command(
                SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.Insert),
                new
                {
                    Id = Guid.NewGuid(),
                    domainEvent.Type,
                    domainEvent.AggregateId,
                    domainEvent.CycleId,
                    domainEvent.Message,
                    OccurredAt = occurredAt,
                    PublishedAt = (DateTimeOffset?)null
                },
                cancellationToken));
        }
        catch (SqlException ex) when (
            domainEvent.Type == DomainEventTypes.MaintenanceDueSoon
            && SqlUniqueness.IsViolation(ex, "UX_Outbox_NoticeCycle"))
        {
            // Aviso único por ciclo: el índice filtrado absorbe la carrera de inserción.
        }
    }
}
