using RutaViva.Domain.ValueObjects;

namespace RutaViva.Domain.Aggregates.Vehicles;

public sealed class Vehicle : AggregateRoot
{
    public string Plate { get; private set; } = "";
    public string Workshop { get; private set; } = "";
    public Kilometres Odometer { get; private set; }

    private Vehicle() { }

    public Vehicle(string plate, string workshop, Kilometres odometer)
    {
        if (string.IsNullOrWhiteSpace(plate) || string.IsNullOrWhiteSpace(workshop))
            throw new BusinessException("Placa y taller son obligatorios.");

        var normalizedPlate = plate.Trim().ToUpperInvariant();
        var normalizedWorkshop = workshop.Trim();
        if (normalizedPlate.Length > 20 || normalizedWorkshop.Length > 120)
            throw new BusinessException("Placa o taller demasiado largos.");

        Id = Guid.NewGuid();
        Plate = normalizedPlate;
        Workshop = normalizedWorkshop;
        Odometer = odometer;
    }

    public bool RecordOdometerReading(Kilometres reportedOdometerKilometers)
    {
        if (reportedOdometerKilometers.IsLessOrEqual(Odometer))
            return false;

        Odometer = reportedOdometerKilometers;
        MarkChanged();
        return true;
    }

    public static Vehicle Reconstitute(
        Guid id,
        long version,
        string plate,
        string workshop,
        decimal odometer) =>
        new()
        {
            Id = id,
            Version = version,
            Plate = plate,
            Workshop = workshop,
            Odometer = new Kilometres(odometer)
        };
}
