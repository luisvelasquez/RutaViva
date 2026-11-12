namespace RutaViva.Domain.ValueObjects;

public sealed record MoneyCop
{
    public const string CurrencyCode = "COP";
    private const decimal MaximumAmount = 9_999_999_999_999_999.99m;

    public decimal Amount { get; }
    public string Currency => CurrencyCode;

    public MoneyCop(decimal amount)
    {
        if (amount <= 0)
            throw new BusinessException("La cotización debe ser mayor que cero.");

        if (amount > MaximumAmount)
            throw new BusinessException($"La cotización no puede superar {MaximumAmount} COP.");

        if (decimal.Round(amount, 2) != amount)
            throw new BusinessException("La cotización debe tener hasta dos decimales.");

        Amount = amount;
    }

    public override string ToString() => $"{Amount:0.##} {CurrencyCode}";
}
