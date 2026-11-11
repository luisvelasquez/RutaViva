using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.ServiceOrders;
using RutaViva.Application.ServiceOrders.Commands;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Queries;
using RutaViva.Domain;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Tags("ServiceOrders")]
public sealed class ServiceOrdersController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists service orders. Optional filters: vehicleId, status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceOrderDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ServiceOrderDto>> ListServiceOrders(
        [FromQuery] Guid? vehicleId,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        mediator.Send(new ListServiceOrdersQuery(vehicleId, status), cancellationToken);

    /// <summary>Reads a service order (read-only).</summary>
    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ServiceOrderDto> GetServiceOrder(Guid orderId, CancellationToken cancellationToken) =>
        mediator.Send(new GetServiceOrderByIdQuery(orderId), cancellationToken);

    /// <summary>
    /// Applies a supported lifecycle operation: quote, approve, reject, cancel, start, or complete.
    /// The aggregate validates the transition; reject and cancel remain distinct decisions.
    /// </summary>
    /// <remarks>
    /// Quote example:
    /// <code>
    /// { "operation": "quote", "expectedVersion": 1, "amount": 1500000 }
    /// </code>
    /// Complete example:
    /// <code>
    /// { "operation": "complete", "expectedVersion": 3, "kilometres": 25100, "date": "2026-11-10" }
    /// </code>
    /// </remarks>
    [HttpPatch("{orderId:guid}")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ServiceOrderDto> ApplyServiceOrderUpdate(
        Guid orderId,
        [FromBody] ApplyServiceOrderUpdateRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            throw new BusinessException("Revise los datos de la operación.");

        var idempotency = IdempotencyHttp.Create(Request, idempotencyKey, request, $"order:{orderId:N}");
        return request.NormalizedOperation switch
        {
            "quote" => mediator.Send(new QuoteServiceOrderCommand(
                orderId, request.ExpectedVersion, request.Amount!.Value, idempotency), cancellationToken),
            "approve" => mediator.Send(new ApproveServiceOrderCommand(
                orderId, request.ExpectedVersion, idempotency), cancellationToken),
            "reject" => mediator.Send(new RejectServiceOrderCommand(
                orderId, request.ExpectedVersion, idempotency), cancellationToken),
            "cancel" => mediator.Send(new CancelServiceOrderCommand(
                orderId, request.ExpectedVersion, idempotency), cancellationToken),
            "start" => mediator.Send(new StartServiceOrderCommand(
                orderId, request.ExpectedVersion, idempotency), cancellationToken),
            "complete" => mediator.Send(new CompleteServiceOrderCommand(
                orderId, request.ExpectedVersion, request.Kilometres!.Value, request.Date!.Value, idempotency),
                cancellationToken),
            _ => throw new BusinessException("Operación no soportada.")
        };
    }
}
