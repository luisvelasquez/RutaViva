namespace RutaViva.Domain.Aggregates;

/// <summary>
/// Clase base abstracta de las raíces de agregado.
/// Comparte identidad, control de versión optimista y recolección de eventos de dominio.
/// Las raíces concretas son Vehicle, MaintenanceRoutine y ServiceOrder.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<Domain.Events.DomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; }
    public long Version { get; protected set; } = 1;

    public IReadOnlyList<Domain.Events.DomainEvent> DomainEvents => _domainEvents;

    protected void MarkChanged() => Version++;

    protected void Raise(Domain.Events.DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        MarkChanged();
    }

    public IReadOnlyList<Domain.Events.DomainEvent> DequeueDomainEvents()
    {
        var copy = _domainEvents.ToArray();
        _domainEvents.Clear();
        return copy;
    }

    public void RequireVersion(long expected)
    {
        if (Version != expected)
            throw new ConflictException("La información cambió. Actualice y vuelva a intentar.");
    }
}
