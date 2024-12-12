using MediatR;

namespace TagTool.BackendNew.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse>;
