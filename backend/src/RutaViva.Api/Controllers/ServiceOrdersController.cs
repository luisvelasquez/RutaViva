using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.ServiceOrders;
using RutaViva.Application.ServiceOrders.Commands;
using RutaViva.Application.ServiceOrders.Dtos;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Tags("ServiceOrders")]
public sealed class ServiceOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/quote")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> QuoteServiceOrder(
        Guid id,
        [FromBody] QuoteServiceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new QuoteServiceOrderCommand(
            id, request.ExpectedVersion, request.Amount,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> ApproveServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new ApproveServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> RejectServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new RejectServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> CancelServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new CancelServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> StartServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new StartServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    public Task<ServiceOrderDto> CompleteServiceOrder(
        Guid id,
        [FromBody] CompleteServiceOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new CompleteServiceOrderCommand(
            id, request.ExpectedVersion, request.Kilometres, request.Date,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{id:N}")), cancellationToken);
}
