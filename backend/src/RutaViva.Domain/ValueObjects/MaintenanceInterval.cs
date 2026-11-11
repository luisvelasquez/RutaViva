namespace RutaViva.Domain.ValueObjects;

/// <summary>Intervalo por kilómetros, días o ambos. Con ambos, vence el primero que se cumpla.</summary>
public sealed record MaintenanceInterval
{
    public Kilometres? IntervalKilometres { get; }
    public int? Days { get; }

    public MaintenanceInterval(decimal? kilometres, int? days)
    {
        if (kilometres is null && days is null)
            throw new BusinessException("Configure kilómetros, días o ambos, mayores a cero.");
        if (kilometres is <= 0 || days is <= 0)
            throw new BusinessException("Configure kilómetros, días o ambos, mayores a cero.");

        IntervalKilometres = kilometres is decimal km ? new Kilometres(km) : null;
        Days = days;
    }

    public bool IsOverdue(Kilometres travelled, int elapsedDays) =>
        (IntervalKilometres is { } limitKm && travelled.Value >= limitKm.Value)
        || (Days is int limitDays && elapsedDays >= limitDays);

    public bool IsDueSoon(Kilometres travelled, int elapsedDays)
    {
        var kmThreshold = IntervalKilometres is { } km && travelled.Value >= km.Value * 0.9m;
        var dayThreshold = Days is int d && elapsedDays >= d * 0.9m;
        return kmThreshold || dayThreshold;
    }
}
