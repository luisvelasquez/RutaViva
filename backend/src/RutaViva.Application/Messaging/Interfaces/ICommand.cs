using MediatR;

namespace RutaViva.Application.Messaging.Interfaces;

public interface ICommand<out TResponse> : IRequest<TResponse>;
