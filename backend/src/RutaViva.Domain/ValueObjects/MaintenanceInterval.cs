
namespace RutaViva.Domain.ValueObjects;

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

        IntervalKilometres = kilometres is decimal km
            ? new Kilometres(km)
            : null;

        Days = days;
    }

    public bool IsOverdue(Kilometres travelled, int elapsedDays)
    {
        bool overdueByKilometres = false;
        bool overdueByDays = false;

        if (IntervalKilometres.HasValue)
        {
            overdueByKilometres = travelled.Value >= IntervalKilometres.Value.Value;
        }

        if (Days.HasValue)
        {
            overdueByDays = elapsedDays >= Days.Value;
        }

        return overdueByKilometres || overdueByDays;
    }

    public bool IsDueSoon(Kilometres travelled, int elapsedDays)
    {
        bool dueSoonByKilometres = false;
        bool dueSoonByDays = false;

        if (IntervalKilometres.HasValue)
        {
            decimal thresholdKm = IntervalKilometres.Value.Value * 0.9m;
            dueSoonByKilometres = travelled.Value >= thresholdKm;
        }

        if (Days.HasValue)
        {
            decimal thresholdDays = Days.Value * 0.9m;
            dueSoonByDays = elapsedDays >= thresholdDays;
        }

        return dueSoonByKilometres || dueSoonByDays;
    }
}
