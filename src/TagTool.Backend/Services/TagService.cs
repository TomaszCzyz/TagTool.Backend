using Grpc.Core;
using MediatR;
using TagTool.Backend.Commands.TagOperations;

namespace TagTool.Backend.Services;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly IMediator _mediator;

    public TagService(IMediator mediator)
    {   
        _mediator = mediator;
    }

    public override async Task<CreateTagsReply> CreateTags(CreateTagsRequest request, ServerCallContext context)
    {
        var results = await _mediator.Send(new CreateTagsCommand { TagNames = request.TagNames.ToArray() });

        return new CreateTagsReply { Results = { results } };
    }

    public override async Task<DeleteTagsReply> DeleteTags(DeleteTagsRequest request, ServerCallContext context)
    {
        var results = await _mediator.Send(new DeleteTagsCommand { TagNames = request.TagNames.ToArray() });

        return new DeleteTagsReply { Results = { results } };
    }

    public override Task Tag(IAsyncStreamReader<TagRequest> requestStream, IServerStreamWriter<TagReply> responseStream,
        ServerCallContext context)
    {
        return base.Tag(requestStream, responseStream, context);
    }

    public override Task Untag(IAsyncStreamReader<UntagRequest> requestStream, IServerStreamWriter<UntagReply> responseStream,
        ServerCallContext context)
    {
        return base.Untag(requestStream, responseStream, context);
    }
}
