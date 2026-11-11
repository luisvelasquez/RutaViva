namespace RutaViva.Domain;

/// <summary>Agregado Orden de servicio: cotización y transiciones legales del flujo.</summary>
public sealed class ServiceOrder : AggregateRoot
{
    public Guid VehicleId { get; private set; }
    public Guid RoutineId { get; private set; }
    public Guid CycleId { get; private set; }
    public string Workshop { get; private set; } = "";
    public OrderState State { get; private set; }
    public MoneyCop? QuotedAmount { get; private set; }
    public Kilometrage? ActualKm { get; private set; }
    public DateOnly? ActualDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsOpen => State is not OrderState.Completed and not OrderState.Cancelled;

    private ServiceOrder() { }

    public ServiceOrder(Guid vehicleId, Guid routineId, Guid cycleId, string workshop, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        RoutineId = routineId;
        CycleId = cycleId;
        Workshop = workshop;
        CreatedAt = now;
        State = OrderState.Created;
    }

    private void RequireState(OrderState expected)
    {
        if (State != expected)
            throw new BusinessException($"La acción requiere estado {expected}; estado actual: {State}.");
    }

    /// <summary>Cotizar: hasta el techo inclusive se aprueba automáticamente.</summary>
    public void Quote(MoneyCop money)
    {
        RequireState(OrderState.Created);
        QuotedAmount = money;
        State = money.QualifiesForAutomaticApproval ? OrderState.Approved : OrderState.Quoted;
        MarkChanged();
    }

    public void Approve()
    {
        RequireState(OrderState.Quoted);
        State = OrderState.Approved;
        MarkChanged();
    }

    public void RejectQuote()
    {
        RequireState(OrderState.Quoted);
        State = OrderState.Cancelled;
        MarkChanged();
    }

    public void Cancel()
    {
        if (State is not (OrderState.Created or OrderState.Quoted or OrderState.Approved))
            throw new BusinessException("Solo puede cancelar antes de iniciar el servicio.");
        State = OrderState.Cancelled;
        MarkChanged();
    }

    public void StartService()
    {
        RequireState(OrderState.Approved);
        State = OrderState.InProgress;
        MarkChanged();
    }

    /// <summary>CompletarServicio con fecha y kilometraje reales.</summary>
    public void CompleteService(Kilometrage km, DateOnly date)
    {
        RequireState(OrderState.InProgress);
        ActualKm = km;
        ActualDate = date;
        State = OrderState.Completed;
        Raise(new DomainEvent(
            DomainEventTypes.ServiceCompleted,
            Id,
            CycleId,
            "Servicio completado y rutina reiniciada."));
    }

    public static ServiceOrder Reconstitute(
        Guid id,
        long version,
        Guid vehicleId,
        Guid routineId,
        Guid cycleId,
        string workshop,
        OrderState state,
        decimal? quoteAmount,
        decimal? actualKm,
        DateOnly? actualDate,
        DateTimeOffset createdAt)
    {
        return new ServiceOrder
        {
            Id = id,
            Version = version,
            VehicleId = vehicleId,
            RoutineId = routineId,
            CycleId = cycleId,
            Workshop = workshop,
            State = state,
            QuotedAmount = quoteAmount is decimal amount ? new MoneyCop(amount) : null,
            ActualKm = actualKm is decimal km ? new Kilometrage(km) : null,
            ActualDate = actualDate,
            CreatedAt = createdAt
        };
    }
}
