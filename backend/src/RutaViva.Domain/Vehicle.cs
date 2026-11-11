namespace RutaViva.Domain;

/// <summary>Agregado Vehículo: placa, taller asignado y odómetro que nunca disminuye.</summary>
public sealed class Vehicle : AggregateRoot
{
    public string Plate { get; private set; } = "";
    public string Workshop { get; private set; } = "";
    public Kilometrage Odometer { get; private set; }

    private Vehicle() { }

    public Vehicle(string plate, string workshop, Kilometrage odometer)
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

    /// <summary>RegistrarLectura: ignora menor o igual sin error; acepta solo avance.</summary>
    public bool RegisterReading(Kilometrage reading)
    {
        if (reading.IsLessOrEqual(Odometer))
            return false;

        Odometer = reading;
        MarkChanged();
        return true;
    }

    public static Vehicle Reconstitute(
        Guid id,
        long version,
        string plate,
        string workshop,
        decimal odometer)
    {
        return new Vehicle
        {
            Id = id,
            Version = version,
            Plate = plate,
            Workshop = workshop,
            Odometer = new Kilometrage(odometer)
        };
    }
}
