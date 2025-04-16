using MediatR;
using OneOf;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Contracts.Invocables;

public interface ITaggableItemOperationBase
{
    static abstract string Name { get; }

    public Guid ItemId { get; set; }
}

// ReSharper disable once UnusedTypeParameter REASON: Type T is used via Reflection in the OperationManager
public interface ITaggableItemOperation<in T, out TResponse> : IRequest<TResponse>, ITaggableItemOperationBase
    where T : TaggableItem
    where TResponse : IOneOf;
