namespace RutaViva.Api.Requests.ServiceOrders;

/// <summary>Completar servicio: ExpectedVersion + Kilometres + Date (contrato Angular).</summary>
public sealed record CompleteServiceOrderRequest(long ExpectedVersion, decimal Kilometres, DateOnly Date);
