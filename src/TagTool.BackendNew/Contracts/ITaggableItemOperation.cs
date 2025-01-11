using MediatR;
using OneOf;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Contracts;

public interface ITaggableItemOperationBase
{
    static abstract string Name { get; }

    public Guid ItemId { get; set; }
}

// ReSharper disable once UnusedTypeParameter REASON: Type T is used with Reflcation in OperationManager
public interface ITaggableItemOperation<in T, out TResponse> : IRequest<TResponse>, ITaggableItemOperationBase
    where T : ITaggableItem
    where TResponse : IOneOf;
