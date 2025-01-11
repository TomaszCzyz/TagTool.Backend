using MediatR;
using OneOf;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.Contracts;

public interface ITaggableItemOperationBase
{
    static abstract string Name { get; }

    public Guid ItemId { get; set; }
}

public interface ITaggableItemOperation<in T, TResponse> : IRequest<TResponse>, ITaggableItemOperationBase
    where T : ITaggableItem
    where TResponse : IOneOf
{
}
