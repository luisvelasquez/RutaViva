using MediatR;

namespace RutaViva.Application.Messaging.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>;
