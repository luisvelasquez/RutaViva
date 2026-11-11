using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Contracts.ServiceOrders;
using RutaViva.Application.ServiceOrders.Commands.ApproveServiceOrder;
using RutaViva.Application.ServiceOrders.Commands.CancelServiceOrder;
using RutaViva.Application.ServiceOrders.Commands.CompleteServiceOrder;
using RutaViva.Application.ServiceOrders.Commands.QuoteServiceOrder;
using RutaViva.Application.ServiceOrders.Commands.RejectServiceOrder;
using RutaViva.Application.ServiceOrders.Commands.StartServiceOrder;
using RutaViva.Application.ServiceOrders.Models;
using RutaViva.Domain;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Tags("ServiceOrders")]
public sealed class ServiceOrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/quote")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> QuoteServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        var amount = request.Amount ?? throw new BusinessException("Indique el valor de la cotización.");
        return mediator.Send(new QuoteServiceOrderCommand(
            id, request.ExpectedVersion, amount,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> ApproveServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        return mediator.Send(new ApproveServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> RejectServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        return mediator.Send(new RejectServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> CancelServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        return mediator.Send(new CancelServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> StartServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        return mediator.Send(new StartServiceOrderCommand(
            id, request.ExpectedVersion,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public Task<OrderDto> CompleteServiceOrder(
        Guid id,
        [FromBody] ServiceOrderActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        var kilometres = request.Kilometres ?? throw new BusinessException("Indique el kilometraje real.");
        var date = request.Date ?? throw new BusinessException("Indique la fecha real.");
        return mediator.Send(new CompleteServiceOrderCommand(
            id, request.ExpectedVersion, kilometres, date,
            IdempotencyHttp.Create(Request, request, $"order:{id:N}")), cancellationToken);
    }
}
