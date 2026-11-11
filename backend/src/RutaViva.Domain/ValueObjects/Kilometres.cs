namespace RutaViva.Domain.ValueObjects;

/// <summary>
/// Kilometraje (odómetro o distancia recorrida). No negativo, hasta dos decimales. Comparación por valor.
/// Sustituye el nombre Kilometres para cubrir ambos usos en el modelo.
/// </summary>
public readonly record struct Kilometres : IComparable<Kilometres>
{
    public decimal Value { get; }

    public Kilometres(decimal value)
    {
        if (value < 0 || value > 9_999_999_999_999_999.99m || decimal.Round(value, 2) != value)
            throw new BusinessException("El kilometraje debe ser no negativo y tener hasta dos decimales.");
        Value = value;
    }

    public int CompareTo(Kilometres other) => Value.CompareTo(other.Value);
    public bool IsGreaterThan(Kilometres other) => Value > other.Value;
    public bool IsLessOrEqual(Kilometres other) => Value <= other.Value;

    public Kilometres DistanceFrom(Kilometres baseline)
    {
        if (Value < baseline.Value)
            throw new BusinessException("El odómetro no puede ser menor que la base del ciclo.");
        return new(Value - baseline.Value);
    }

    public override string ToString() => $"{Value} km";
    public static implicit operator decimal(Kilometres km) => km.Value;
}
