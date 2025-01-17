using Grpc.Core;
using MediatR;
using OneOf.Types;
using TagTool.BackendNew.Commands;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Models;
using TagTool.BackendNew.Queries;

namespace TagTool.BackendNew.Services.Grpc;

public class TagService : BackendNew.TagService.TagServiceBase
{
    private readonly ILogger<TagService> _logger;
    private readonly IMediator _mediator;
    private readonly IOperationManger _operationManger;

    public TagService(ILogger<TagService> logger, IMediator mediator, IOperationManger operationManger)
    {
        _logger = logger;
        _mediator = mediator;
        _operationManger = operationManger;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Text);

        var command = new CreateTag { Text = request.Text };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new CreateTagReply { Tag = new Tag { Id = tagBase.Id, Text = tagBase.Text } },
            error => new CreateTagReply { ErrorMessage = error.Value });
    }

    public override async Task CanCreateTag(
        IAsyncStreamReader<CanCreateTagRequest> requestStream,
        IServerStreamWriter<CanCreateTagReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var request = requestStream.Current;

            if (string.IsNullOrEmpty(request.TagName))
            {
                await responseStream.WriteAsync(new CanCreateTagReply { Error = new Error { Message = "Tag name cannot be empty." } });
                continue;
            }

            var query = new CanCreateTag { NewTagText = request.TagName };

            var response = await _mediator.Send(query, context.CancellationToken);

            var reply = response.Match(
                _ => new CanCreateTagReply(),
                error => new CanCreateTagReply { Error = new Error { Message = error.Value } });

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var command = new Commands.DeleteTagRequest { Id = request.TagId, DeleteUsedToo = request.DeleteUsedToo };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new DeleteTagReply { Tag = tagBase.ToDto() },
            error => new DeleteTagReply { ErrorMessage = error.Value });
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.ItemId);
        ArgumentNullException.ThrowIfNull(request.TagId);

        var command = new TagItem { ItemId = new Guid(request.ItemId), TagId = new Guid(request.TagId) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply { Item = item.ToDto() },
            error => new TagItemReply { ErrorMessage = error.Value });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.ItemId);
        ArgumentNullException.ThrowIfNull(request.TagId);

        var command = new UntagItem { ItemId = new Guid(request.ItemId), TagId = new Guid(request.TagId) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply { TaggedItem = item.ToDto() },
            error => new UntagItemReply { ErrorMessage = error.Value });
    }

    public override async Task<InvokeOperationReply> InvokeOperation(InvokeOperationRequest request, ServerCallContext context)
    {
        var result = await _operationManger.InvokeOperation(request.ItemId, request.OperationName, request.OperationArgs);

        if (result.Value is Error<string> error)
        {
            _logger.LogInformation(
                "Invoking operation {OperationName} returned result {OperationResult}",
                request.OperationName,
                error.Value);
        }
        else
        {
            _logger.LogInformation(
                "Invoking operation {OperationName} returned result {OperationResult}",
                request.OperationName,
                result.Value.ToString());
        }


        return new InvokeOperationReply();
    }

    public override async Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var query = new GetItemById { Id = new Guid(request.ItemId) };
        var response = await _mediator.Send(query, context.CancellationToken);

        return response.Match(
            item => new GetItemReply { TaggedItem = item.ToDto() },
            _ => new GetItemReply { ErrorMessage = $"Could not find taggable item with id {request.ItemId}." });
    }


// public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
// {
//     ArgumentNullException.ThrowIfNull(request.QueryParams);
//
//     if (request.QueryParams.Any(param => param.Tag is null))
//     {
//         throw new ArgumentNullException(nameof(request), "No tag in a tag query can be null");
//     }
//
//     // todo: add validation - tag cannot be null
//     var querySegments = request.QueryParams
//         .Select(param => new TagQuerySegment { State = MapQuerySegmentStateFromDto(param.State), Tag = _tagMapper.MapFromDto(param.Tag) })
//         .ToArray();
//
//     var query = new GetItemsByTagsQuery { QuerySegments = querySegments };
//
//     var response = await _mediator.Send(query, context.CancellationToken);
//
//     return new GetItemsByTagsReply { TaggedItems = { response.Select(MapItem) } };
// }
    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Text);

        var doesTagExistsQuery = new GetTagByText { Text = request.Text };

        var response = await _mediator.Send(doesTagExistsQuery, context.CancellationToken);

        return response.Match(
            tag => new DoesTagExistsReply { Tag = tag.ToDto() },
            _ => new DoesTagExistsReply());
    }

    public override async Task SearchTags(
        SearchTagsRequest request,
        IServerStreamWriter<SearchTagsReply> responseStream,
        ServerCallContext context)
    {
        var (value, limit) = (request.SearchText, request.ResultsLimit);

        IStreamRequest<(TagBase, IEnumerable<TextSlice>)> query = request.SearchType switch
        {
            SearchTagsRequest.Types.SearchType.Wildcard => new SearchTagsWildcardRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.StartsWith => new SearchTagsStartsWithRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.Fuzzy => new SearchTagsFuzzyRequest { Value = value, ResultsLimit = limit },
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        await foreach (var (tag, parts) in _mediator.CreateStream(query, context.CancellationToken))
        {
            var matchedParts = parts
                .Select(part => new SearchTagsReply.Types.MatchedPart { StartIndex = part.StartIndex, Length = part.Length })
                .ToArray();

            var matchTagsReply = new SearchTagsReply
            {
                Tag = tag.ToDto(),
                MatchedPart = { matchedParts },
                IsExactMatch = matchedParts[0].Length == tag.Text.Length
            };

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
        }
    }

// public override async Task<UndoReply> Undo(UndoRequest request, ServerCallContext context)
// {
//     var undoCommand = _commandsHistory.GetUndoCommand();
//
//     var result = await InvokeCommand(nameof(Undo), undoCommand, context.CancellationToken);
//
//     return result.Match(
//         s => new UndoReply { UndoCommand = s },
//         errorResponse => new UndoReply { ErrorMessage = errorResponse.Message });
// }
//
// public override async Task<RedoReply> Redo(RedoRequest request, ServerCallContext context)
// {
//     var redoCommand = _commandsHistory.GetRedoCommand();
//
//     var result = await InvokeCommand(nameof(Redo), redoCommand, context.CancellationToken);
//
//     return result.Match(
//         s => new RedoReply { RedoCommand = s },
//         errorResponse => new RedoReply { ErrorMessage = errorResponse.Message });
// }

// private static QuerySegmentState MapQuerySegmentStateFromDto(TagQueryParam.Types.QuerySegmentState state)
//     => state switch
//     {
//         TagQueryParam.Types.QuerySegmentState.Exclude => QuerySegmentState.Exclude,
//         TagQueryParam.Types.QuerySegmentState.Include => QuerySegmentState.Include,
//         TagQueryParam.Types.QuerySegmentState.MustBePresent => QuerySegmentState.MustBePresent,
//         _ => throw new UnreachableException()
//     };
//
// private static TagQueryParam.Types.QuerySegmentState MapQuerySegmentStateToDto(QuerySegmentState querySegment)
//     => querySegment switch
//     {
//         QuerySegmentState.Exclude => TagQueryParam.Types.QuerySegmentState.Exclude,
//         QuerySegmentState.Include => TagQueryParam.Types.QuerySegmentState.Include,
//         QuerySegmentState.MustBePresent => TagQueryParam.Types.QuerySegmentState.MustBePresent,
//         _ => throw new UnreachableException()
//     };
}

public static class TagBaseExtensions
{
    public static Tag ToDto(this TagBase tag) => new() { Id = tag.Id, Text = tag.Text };
}

public static class TaggableItemExtensions
{
    public static TaggedItem ToDto(this TaggableItem item) => new() { Id = item.Id.ToString(), Tags = { item.Tags.Select(t => t.ToDto()) } };
}
