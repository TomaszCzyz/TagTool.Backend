using System.Text.Json;
using MediatR;

namespace TagTool.BackendNew.Entities;

internal class TaggableFile : ITaggableItem
{
    public ICollection<TagBase> Tags { get; } = [];

    // attributes?
}

// ==========

internal interface ITaggableItemOperationBase;

internal interface ITaggableItemOperation<in T, in TPayload> : ITaggableItemOperationBase
    where T : ITaggableItem
{
    string Name { get; }

    Task Invoke(T item, TPayload payload);
}

internal class TaggableFileMove : ITaggableItemOperation<TaggableFile, Unit>
{
    public string Name { get; } = "file:move";

    public Task Invoke(TaggableFile item, Unit payload)
    {
        throw new NotImplementedException();
    }
}

internal record TaggableFileRenameOperationPayload(string NewName);

internal interface ITaggableFileOperation<TPayload>
    : ITaggableItemOperation<TaggableFile, TPayload>, IRequest<TPayload>
    where TPayload : class;

internal class TaggableFileRenameOperation : ITaggableFileOperation<TaggableFileRenameOperationPayload>
{
    public string Name { get; } = "file:rename";

    public Task Invoke(TaggableFile item, TaggableFileRenameOperationPayload operationPayload)
    {
        throw new NotImplementedException();
    }
}

internal class TaggableFileRenameOperationHandler : IRequestHandler<TaggableFileRenameOperation, TaggableFileRenameOperationPayload>
{
    public Task<TaggableFileRenameOperationPayload> Handle(TaggableFileRenameOperation request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal class OperationManger
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;

    public OperationManger(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        var services = _serviceProvider.GetServices(typeof(ITaggableItemOperation<,>));

        foreach (var service in services)
        {
            // service.
        }
    }

    // GetFileOperations() => [{"file:move", schema}, {"file:create", schema}]
    private async Task GetOperationNames<T>(string taggableItemType) where T : ITaggableItem
    {
    }

    public async Task InvokeOperation(string operationName, string operationPayload)
    {
        var payloadType = GetPayloadType(operationName);
        var payload = JsonSerializer.Deserialize(operationPayload, payloadType);

        if (payload is null)
        {
            throw new InvalidOperationException($"Operation '{operationName}' is not supported");
        }

        var send = await _mediator.Send(payload);
    }

    private Type GetPayloadType(string operationName)
    {
        throw new NotImplementedException();
    }
}

// IAction<>
