namespace RutaViva.Domain;

public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; }
    public long Version { get; protected set; } = 1;

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    protected void MarkChanged() => Version++;

    protected void Raise(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        MarkChanged();
    }

    public IReadOnlyList<DomainEvent> DequeueDomainEvents()
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
