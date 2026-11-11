namespace RutaViva.Api.Requests.ServiceOrders;

/// <summary>Cotización: ExpectedVersion + Amount (misma forma JSON que usa Angular).</summary>
public sealed record QuoteServiceOrderRequest(long ExpectedVersion, decimal Amount);
