namespace RutaViva.Domain.ValueObjects;

/// <summary>
/// Importe en COP. Comparación por valor. No admite default inválido: es clase (null o instancia válida).
/// El techo de autoaprobación no vive aquí: es regla de la orden de servicio.
/// </summary>
public sealed class MoneyCop : IEquatable<MoneyCop>
{
    public const string CurrencyCode = "COP";

    public decimal Amount { get; }
    public string Currency => CurrencyCode;

    public MoneyCop(decimal amount)
    {
        if (amount <= 0 || amount > 9_999_999_999_999_999.99m || decimal.Round(amount, 2) != amount)
            throw new BusinessException("La cotización debe ser positiva y tener hasta dos decimales.");
        Amount = amount;
    }

    public bool Equals(MoneyCop? other) => other is not null && Amount == other.Amount;
    public override bool Equals(object? obj) => obj is MoneyCop other && Equals(other);
    public override int GetHashCode() => Amount.GetHashCode();
    public override string ToString() => $"{Amount:0.##} {CurrencyCode}";
}
