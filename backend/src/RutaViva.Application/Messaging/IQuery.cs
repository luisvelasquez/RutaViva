using MediatR;

namespace RutaViva.Application.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
