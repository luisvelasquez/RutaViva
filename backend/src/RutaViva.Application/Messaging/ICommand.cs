using MediatR;

namespace RutaViva.Application.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>;
