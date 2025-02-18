﻿using MediatR;
using OneOf;

namespace TagTool.BackendNew.Contracts.Internal;

public interface ICommand<out TResponse> : IRequest<TResponse> where TResponse : IOneOf;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse> where TResponse : IOneOf;
