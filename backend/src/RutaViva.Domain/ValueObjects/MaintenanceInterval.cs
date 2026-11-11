namespace RutaViva.Domain;

/// <summary>Intervalo por kilómetros, días o ambos. Con ambos, vence el primero que se cumpla.</summary>
public sealed record MaintenanceInterval
{
    public Kilometrage? Kilometres { get; }
    public int? Days { get; }

    public MaintenanceInterval(decimal? kilometres, int? days)
    {
        if (kilometres is null && days is null)
            throw new BusinessException("Configure kilómetros, días o ambos, mayores a cero.");
        if (kilometres is <= 0 || days is <= 0)
            throw new BusinessException("Configure kilómetros, días o ambos, mayores a cero.");

        Kilometres = kilometres is decimal km ? new Kilometrage(km) : null;
        Days = days;
    }

    public bool IsOverdue(Kilometrage travelled, int elapsedDays) =>
        (Kilometres is Kilometrage limitKm && travelled.Value >= limitKm.Value)
        || (Days is int limitDays && elapsedDays >= limitDays);

    public bool IsDueSoon(Kilometrage travelled, int elapsedDays)
    {
        var kmThreshold = Kilometres is Kilometrage km && travelled.Value >= km.Value * 0.9m;
        var dayThreshold = Days is int d && elapsedDays >= d * 0.9m;
        return kmThreshold || dayThreshold;
    }
}
