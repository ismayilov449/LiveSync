using MediatR;

namespace LiveSync.Application.Common.Interfaces;

public interface ICommand : IRequest;
public interface ICommand<out TResponse> : IRequest<TResponse>;
