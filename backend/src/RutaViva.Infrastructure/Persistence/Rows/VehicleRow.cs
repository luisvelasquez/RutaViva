namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class VehicleRow
{
    public Guid Id { get; init; }
    public long Version { get; init; }
    public string Plate { get; init; } = "";
    public string Workshop { get; init; } = "";
    public decimal Odometer { get; init; }
}
