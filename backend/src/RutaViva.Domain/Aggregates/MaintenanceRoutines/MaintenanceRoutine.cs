using RutaViva.Domain.Events;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Domain.Aggregates.MaintenanceRoutines;

public sealed class MaintenanceRoutine : AggregateRoot
{
    public Guid VehicleId { get; private set; }
    public string Name { get; private set; } = "";
    public MaintenanceInterval Interval { get; private set; } = null!;
    public Kilometres LastServiceKm { get; private set; }
    public DateOnly LastServiceDate { get; private set; }
    public Guid CycleId { get; private set; }
    public bool NoticeRaised { get; private set; }
    public bool CycleCancelled { get; private set; }
    public MaintenanceStatus Status { get; private set; }

    private MaintenanceRoutine() { }

    public MaintenanceRoutine(
        Guid vehicleId,
        string name,
        MaintenanceInterval interval,
        Kilometres baseline,
        DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new BusinessException("Nombre de rutina inválido.");

        Id = Guid.NewGuid();
        VehicleId = vehicleId;
        Name = name.Trim();
        Interval = interval;
        LastServiceKm = baseline;
        LastServiceDate = date;
        CycleId = Guid.NewGuid();
        Status = MaintenanceStatus.Current;
    }

    public void EvaluateDueStatus(Kilometres odometer, DateOnly today)
    {
        if (CycleCancelled)
            return;

        var travelled = odometer.DistanceFrom(LastServiceKm);
        var elapsedDays = today.DayNumber - LastServiceDate.DayNumber;
        var next = ResolveStatus(travelled, elapsedDays);

        if (next != Status)
        {
            Status = next;
            MarkChanged();
        }

        // Salto directo a vencida: no se emite aviso tardío de «por vencer».
        if (next != MaintenanceStatus.DueSoon || NoticeRaised)
            return;

        NoticeRaised = true;
        Raise(DomainEventFactory.MaintenanceDueSoon(Id, CycleId, Name));
    }

    private MaintenanceStatus ResolveStatus(Kilometres travelled, int elapsedDays)
    {
        if (Interval.IsOverdue(travelled, elapsedDays))
            return MaintenanceStatus.Overdue;
        if (Interval.IsDueSoon(travelled, elapsedDays))
            return MaintenanceStatus.DueSoon;
        return MaintenanceStatus.Current;
    }

    public void SuppressCancelledCycle()
    {
        CycleCancelled = true;
        MarkChanged();
    }

    public void ResetMaintenanceCycle(Kilometres serviceKm, DateOnly serviceDate, DateOnly today)
    {
        if (serviceKm.Value < LastServiceKm.Value || serviceDate < LastServiceDate || serviceDate > today)
            throw new BusinessException("El servicio no puede preceder al anterior ni tener fecha futura.");

        LastServiceKm = serviceKm;
        LastServiceDate = serviceDate;
        CycleId = Guid.NewGuid();
        NoticeRaised = false;
        CycleCancelled = false;
        Status = MaintenanceStatus.Current;
        MarkChanged();
    }

    public static MaintenanceRoutine Reconstitute(
        Guid id,
        long version,
        Guid vehicleId,
        string name,
        decimal? intervalKm,
        int? intervalDays,
        decimal lastServiceKm,
        DateOnly lastServiceDate,
        Guid cycleId,
        bool noticeRaised,
        bool cycleCancelled,
        MaintenanceStatus status) =>
        new()
        {
            Id = id,
            Version = version,
            VehicleId = vehicleId,
            Name = name,
            Interval = new MaintenanceInterval(intervalKm, intervalDays),
            LastServiceKm = new Kilometres(lastServiceKm),
            LastServiceDate = lastServiceDate,
            CycleId = cycleId,
            NoticeRaised = noticeRaised,
            CycleCancelled = cycleCancelled,
            Status = status
        };
}
