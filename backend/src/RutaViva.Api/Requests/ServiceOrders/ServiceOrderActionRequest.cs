namespace RutaViva.Api.Requests.ServiceOrders;

/// <summary>Cuerpo compartido por las acciones de orden (compatible con Angular).</summary>
public sealed record ServiceOrderActionRequest(
    long ExpectedVersion,
    decimal? Amount = null,
    decimal? Kilometres = null,
    DateOnly? Date = null);
