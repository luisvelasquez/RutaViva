namespace RutaViva.Domain.ValueObjects;

/// <summary>Dinero en COP. Importe positivo con hasta dos decimales.</summary>
public readonly record struct MoneyCop
{
    public const string CurrencyCode = "COP";
    public static readonly MoneyCop AutoApprovalCeiling = new(1_500_000m);

    public decimal Amount { get; }
    public string Currency => CurrencyCode;

    public MoneyCop(decimal amount)
    {
        if (amount <= 0 || amount > 9_999_999_999_999_999.99m || decimal.Round(amount, 2) != amount)
            throw new BusinessException("La cotización debe ser positiva y tener hasta dos decimales.");
        Amount = amount;
    }

    public bool QualifiesForAutomaticApproval => Amount <= AutoApprovalCeiling.Amount;
    public override string ToString() => $"{Amount:0.##} {CurrencyCode}";
}
