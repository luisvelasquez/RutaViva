using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RutaViva.Api.Requests.ServiceOrders;

/// <summary>
/// PATCH contract for service-order lifecycle operations.
/// <c>operation</c> is required; amount is required for quote; kilometres and date for complete.
/// </summary>
public sealed class ApplyServiceOrderUpdateRequest : IValidatableObject
{
    [Required]
    public string Operation { get; init; } = "";

    [Required]
    public long ExpectedVersion { get; init; }

    /// <summary>Required when operation is quote (COP).</summary>
    public decimal? Amount { get; init; }

    /// <summary>Required when operation is complete (actual service odometer).</summary>
    public decimal? Kilometres { get; init; }

    /// <summary>Required when operation is complete (actual service date).</summary>
    public DateOnly? Date { get; init; }

    [JsonIgnore]
    public string NormalizedOperation => Operation.Trim().ToLowerInvariant();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (NormalizedOperation)
        {
            case "quote":
                if (Amount is null or <= 0)
                    yield return new ValidationResult("Cotizar requiere un importe positivo.", [nameof(Amount)]);
                break;
            case "approve":
            case "reject":
            case "cancel":
            case "start":
                break;
            case "complete":
                if (Kilometres is null or < 0)
                    yield return new ValidationResult("Completar requiere el kilometraje real del servicio.", [nameof(Kilometres)]);
                if (Date is null)
                    yield return new ValidationResult("Completar requiere la fecha real del servicio.", [nameof(Date)]);
                break;
            default:
                yield return new ValidationResult(
                    "Operación no soportada. Use quote, approve, reject, cancel, start o complete.",
                    [nameof(Operation)]);
                break;
        }
    }
}
