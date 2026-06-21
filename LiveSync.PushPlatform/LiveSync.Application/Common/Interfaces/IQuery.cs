using MediatR;

namespace LiveSync.Application.Common.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>;
