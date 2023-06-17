﻿using Grpc.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.Commands;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Mappers;
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
        var tag = TagMapper.MapToDomain(request.Tag);

        var command = new Commands.CreateTagRequest { Tag = tag };
        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            newTagName => new CreateTagReply { CreatedTagName = newTagName.FormattedName },
            errorResponse => new CreateTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var tag = TagMapper.MapToDomain(request.Tag);

        var command = new Commands.DeleteTagRequest { Tag = tag };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            deletedTagName => new DeleteTagReply { DeletedTagName = deletedTagName },
            errorResponse => new DeleteTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        var tagBase = TagMapper.MapToDomain(request.Tag);

        var command = new Commands.TagItemRequest
        {
            Tag = tagBase,
            ItemType = request.Item.ItemType,
            Identifier = request.Item.Identifier
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply { TaggedItem = new TaggedItem { Item = request.Item, Tags = { item.Tags.Select(TagMapper.MapToDto) } } },
            errorResponse => new TagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        var tagBase = TagMapper.MapToDomain(request.Tag);

        var command = new Commands.UntagItemRequest
        {
            Tag = tagBase,
            ItemType = request.Item.ItemType,
            Identifier = request.Item.Identifier
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply { TaggedItem = new TaggedItem { Item = request.Item, Tags = { item.Tags.Select(TagMapper.MapToDto) } } },
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
            : new GetItemReply { TaggedItem = new TaggedItem { Item = request.Item, Tags = { existingItem.Tags.Select(TagMapper.MapToDto) } } };
    }

    public override async Task<GetItemsByTagsV2Reply> GetItemsByTagsV2(GetItemsByTagsV2Request request, ServerCallContext context)
    {
        var querySegments = request.QueryParams
            .Select(tagQueryParam
                => new TagQuerySegment
                {
                    Include = tagQueryParam.Include,
                    MustBePresent = tagQueryParam.MustBePresent,
                    Tag = TagMapper.MapToDomain(tagQueryParam.Tag) //MapToTag(tagQueryParam.TagType, tagQueryParam.Params.ToArray()),
                })
            .ToList();

        var getItemsByTagsV2Query = new GetItemsByTagsV2Query { QuerySegments = querySegments };
        var response = await _mediator.Send(getItemsByTagsV2Query);

        var results = response
            .Select(item =>
                new TaggedItem
                {
                    Item = new Item { ItemType = item.ItemType, Identifier = item.UniqueIdentifier },
                    Tags = { item.Tags.Select(TagMapper.MapToDto) }
                })
            .ToArray();

        return new GetItemsByTagsV2Reply { TaggedItems = { results } };
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
        var tag = TagMapper.MapToDomain(request.Tag);
        var existingItem = await _dbContext.Tags
            .FirstOrDefaultAsync(tagBase => tagBase.FormattedName == tag.FormattedName, context.CancellationToken);

        return new DoesTagExistsReply { Exists = existingItem is not null };
    }

    public override async Task SearchTags(
        SearchTagsRequest request,
        IServerStreamWriter<SearchTagsReply> responseStream,
        ServerCallContext context)
    {
        var (value, limit) = (request.Name, request.ResultsLimit);

        IStreamRequest<(TagBase, IEnumerable<MatchedPart>)> query = request.SearchType switch
        {
            SearchTagsRequest.Types.SearchType.Wildcard => new SearchTagsWildcardRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.StartsWith => new SearchTagsStartsWithRequest { Value = value, ResultsLimit = limit },
            SearchTagsRequest.Types.SearchType.Partial => new SearchTagsPartialRequest { Value = value, ResultsLimit = limit },
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        await foreach (var (tag, parts) in _mediator.CreateStream(query, context.CancellationToken))
        {
            var matchedParts = parts
                .Select(part => new SearchTagsReply.Types.MatchedPart { StartIndex = part.StartIndex, Length = part.Length })
                .ToArray();

            var matchTagsReply = new SearchTagsReply
            {
                Tag = TagMapper.MapToDto(tag),
                MatchedPart = { matchedParts },
                IsExactMatch = matchedParts[0].Length == tag.FormattedName.Length
            };

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
        }
    }

    public override async Task<UndoReply> Undo(UndoRequest request, ServerCallContext context)
    {
        var undoCommand = _commandsHistory.GetUndoCommand();

        var result = await InvokeCommand(nameof(Undo), undoCommand);

        return result.Match(
            s => new UndoReply { UndoCommand = s },
            errorResponse => new UndoReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<RedoReply> Redo(RedoRequest request, ServerCallContext context)
    {
        var redoCommand = _commandsHistory.GetRedoCommand();

        var result = await InvokeCommand(nameof(Redo), redoCommand);

        return result.Match(
            s => new RedoReply { RedoCommand = s },
            errorResponse => new RedoReply { ErrorMessage = errorResponse.Message });
    }

    private async Task<OneOf<string, ErrorResponse>> InvokeCommand(string undoOrRedo, IReversible? command)
    {
        if (command is null) return new ErrorResponse($"Nothing to {undoOrRedo}.");

        var response = await _mediator.Send(command);

        if (response is IOneOf { Value: ErrorResponse errorResponse })
        {
            _logger.LogWarning("Invoking of a command {@Command} was unsuccessful. Error: {@ErrorResponse}", command, errorResponse);
            return new ErrorResponse($"Command {command} was not reverted");
        }

        return command.GetType().ToString();
    }

    public override async Task<SetTagNamingConventionReply> SetTagNamingConvention(
        SetTagNamingConventionRequest request,
        ServerCallContext context)
    {
        var setTagNamingConventionCommand = new SetTagNamingConventionCommand { NewNamingConvention = Map(request.Convention) };

        var oneOf = await _mediator.Send(setTagNamingConventionCommand);

        return oneOf.Match(
            _ => new SetTagNamingConventionReply(),
            response => new SetTagNamingConventionReply { Error = new Error { Message = response.Message } });
    }

    private static Models.Options.NamingConvention Map(NamingConvention requestConvention)
    {
        return requestConvention switch
        {
            NamingConvention.None => Models.Options.NamingConvention.Unchanged,
            NamingConvention.CamelCase => Models.Options.NamingConvention.CamelCase,
            NamingConvention.PascalCase => Models.Options.NamingConvention.PascalCase,
            NamingConvention.KebabCase => Models.Options.NamingConvention.KebabCase,
            NamingConvention.SnakeCase => Models.Options.NamingConvention.SnakeCase,
            _ => throw new ArgumentOutOfRangeException(nameof(requestConvention), requestConvention, null)
        };
    }
}
