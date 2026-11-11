namespace RutaViva.Domain.ValueObjects;

/// <summary>Kilometraje no negativo con hasta dos decimales. Comparación por valor.</summary>
public readonly record struct Kilometrage : IComparable<Kilometrage>
{
    public decimal Value { get; }

    public Kilometrage(decimal value)
    {
        if (value < 0 || value > 9_999_999_999_999_999.99m || decimal.Round(value, 2) != value)
            throw new BusinessException("El kilometraje debe ser no negativo y tener hasta dos decimales.");
        Value = value;
    }

    public int CompareTo(Kilometrage other) => Value.CompareTo(other.Value);
    public bool IsGreaterThan(Kilometrage other) => Value > other.Value;
    public bool IsLessOrEqual(Kilometrage other) => Value <= other.Value;

    public Kilometrage DistanceFrom(Kilometrage baseline)
    {
        if (Value < baseline.Value)
            throw new BusinessException("El odómetro no puede ser menor que la base del ciclo.");
        return new(Value - baseline.Value);
    }

    public override string ToString() => $"{Value} km";
    public static implicit operator decimal(Kilometrage km) => km.Value;
}
