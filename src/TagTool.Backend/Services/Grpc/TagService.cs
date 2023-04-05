using Grpc.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using TagTool.Backend.Queries;
using TaggedItem = TagTool.Backend.DomainTypes.TaggedItem;

namespace TagTool.Backend.Services.Grpc;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ILogger<TagService> _logger;
    private readonly IMediator _mediator;
    private readonly ICommandsHistory _commandsHistory;
    private readonly TagToolDbContext _dbContext;

    public TagService(ILogger<TagService> logger, IMediator mediator, ICommandsHistory commandsHistory, TagToolDbContext dbContext)
    {
        _logger = logger;
        _mediator = mediator;
        _commandsHistory = commandsHistory;
        _dbContext = dbContext;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var command = new Commands.CreateTagRequest { TagName = request.TagName };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            newTagName => new CreateTagReply { CreatedTagName = newTagName },
            errorResponse => new CreateTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var command = new Commands.CreateTagRequest { TagName = request.TagName };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            deletedTagName => new DeleteTagReply { DeletedTagName = deletedTagName },
            errorResponse => new DeleteTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        var command = new Commands.TagItemRequest
        {
            TagName = request.TagName,
            ItemType = request.Item.ItemType,
            Identifier = request.Item.Identifier
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { item.Tags.Names() } } },
            errorResponse => new TagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        var command = new Commands.UntagItemRequest
        {
            TagName = request.TagName,
            ItemType = request.Item.ItemType,
            Identifier = request.Item.Identifier
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { item.Tags.Names() } } },
            errorResponse => new UntagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);
        var existingItem = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        return existingItem is null
            ? new GetItemReply { ErrorMessage = $"Requested item {request.Item} does not exists." }
            : new GetItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { existingItem.Tags.Select(tag => tag.Name) } } };
    }

    public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
    {
        var query = new Queries.GetItemsByTagsRequest { TagNames = request.TagNames.ToArray() };

        var taggedItems = await _mediator.Send(query, context.CancellationToken);

        var results = taggedItems
            .Select(item =>
                new TaggedItem
                {
                    Item = new Item { ItemType = item.ItemType, Identifier = item.UniqueIdentifier }, TagNames = { item.Tags.Names() }
                })
            .ToArray();

        return new GetItemsByTagsReply { TaggedItem = { results } };
    }

    public override async Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);
        var existingItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier, context.CancellationToken);

        return new DoesItemExistsReply { Exists = existingItem is not null };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        var existingItem = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Name == request.TagName, context.CancellationToken);

        return new DoesTagExistsReply { Exists = existingItem is not null };
    }

    public override async Task SearchTags(
        SearchTagsRequest request,
        IServerStreamWriter<SearchTagsReply> responseStream,
        ServerCallContext context)
    {
        var (value, limit) = (request.Name, request.ResultsLimit);

        IStreamRequest<(string, IEnumerable<MatchedPart>)> query = request.SearchType switch
        {
            SearchTagsRequest.Types.SearchType.Wildcard => new SearchTagsWildcardRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.StartsWith => new SearchTagsStartsWithRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.Partial => new SearchTagsPartialRequest { Value = value, ResultsLimit = limit },
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        await foreach (var (tagName, parts) in _mediator.CreateStream(query, context.CancellationToken))
        {
            var matchedParts = parts
                .Select(part => new SearchTagsReply.Types.MatchedPart { StartIndex = part.StartIndex, Length = part.Length })
                .ToArray();

            var matchTagsReply = new SearchTagsReply
            {
                TagName = tagName,
                MatchedPart = { matchedParts },
                IsExactMatch = matchedParts[0].Length == tagName.Length
            };

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
        }
    }

    public override async Task<UndoReply> Undo(UndoRequest request, ServerCallContext context)
    {
        var baseRequest = _commandsHistory.Pop();
        var response = await _mediator.Send(baseRequest, context.CancellationToken);

        if (response is IOneOf { Value: ErrorResponse errorResponse })
        {
            _logger.LogWarning("Undo of command {@Command} was unsuccessful. Error: {@ErrorResponse}", baseRequest, errorResponse);
        }

        return new UndoReply();
    }
}
