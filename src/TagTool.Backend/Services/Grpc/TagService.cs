using System.Diagnostics;
using Grpc.Core;
using MediatR;
using OneOf;
using TagTool.Backend.Commands;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;

namespace TagTool.Backend.Services.Grpc;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ILogger<TagService> _logger;
    private readonly IMediator _mediator;
    private readonly ICommandsHistory _commandsHistory;
    private readonly ITagMapper _tagMapper;

    public TagService(ILogger<TagService> logger, IMediator mediator, ICommandsHistory commandsHistory, ITagMapper tagMapper)
    {
        _logger = logger;
        _mediator = mediator;
        _commandsHistory = commandsHistory;
        _tagMapper = tagMapper;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var tag = _tagMapper.MapFromDto(request.Tag);

        var command = new Commands.CreateTagRequest { Tag = tag };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new CreateTagReply { CreatedTagName = tagBase.FormattedName },
            errorResponse => new CreateTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task CanCreateTag(
        IAsyncStreamReader<CanCreateTagRequest> requestStream,
        IServerStreamWriter<CanCreateTagReply> responseStream,
        ServerCallContext context)
    {
        while (await requestStream.MoveNext())
        {
            var canCreateTagRequest = requestStream.Current;

            var query = new CanCreateTagQuery { NewTagName = canCreateTagRequest.TagName };

            var response = await _mediator.Send(query, context.CancellationToken);

            var reply = response.Match(
                errorResponse => new CanCreateTagReply { Error = new Error { Message = errorResponse.Message } },
                _ => new CanCreateTagReply());

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var tag = _tagMapper.MapFromDto(request.Tag);

        var command = new Commands.DeleteTagRequest { Tag = tag };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            deletedTagName => new DeleteTagReply { DeletedTagName = deletedTagName },
            errorResponse => new DeleteTagReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task GetAllTagsAssociations(
        GetAllTagsAssociationsRequest request,
        IServerStreamWriter<GetAllTagsAssociationsReply> responseStream,
        ServerCallContext context)
    {
        var query = new GetAllTagsAssociationsQuery { TagBase = request.Tag is not null ? _tagMapper.MapFromDto(request.Tag) : null };

        var asyncEnumerable = _mediator.CreateStream(query, context.CancellationToken);

        await foreach (var groupDescription in asyncEnumerable)
        {
            await responseStream.WriteAsync(
                new GetAllTagsAssociationsReply
                {
                    GroupName = groupDescription.GroupName,
                    TagsInGroup = { groupDescription.GroupTags.Select(tagBase => _tagMapper.MapToDto(tagBase)) },
                    ParentGroupNames = { groupDescription.GroupAncestors }
                });
        }
    }

    public override async Task<AddSynonymReply> AddSynonym(AddSynonymRequest request, ServerCallContext context)
    {
        var command = new Commands.AddSynonymRequest { GroupName = request.GroupName, Tag = _tagMapper.MapFromDto(request.Tag) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new AddSynonymReply { SuccessMessage = s },
            errorResponse => new AddSynonymReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<RemoveSynonymReply> RemoveSynonym(RemoveSynonymRequest request, ServerCallContext context)
    {
        var command = new Commands.RemoveSynonymRequest { GroupName = request.GroupName, Tag = _tagMapper.MapFromDto(request.Tag) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new RemoveSynonymReply { SuccessMessage = s },
            errorResponse => new RemoveSynonymReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<AddChildReply> AddChild(AddChildRequest request, ServerCallContext context)
    {
        var command = new Commands.AddChildRequest
        {
            ChildTag = _tagMapper.MapFromDto(request.ChildTag), ParentTag = _tagMapper.MapFromDto(request.ParentTag)
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new AddChildReply { SuccessMessage = s },
            errorResponse => new AddChildReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<RemoveChildReply> RemoveChild(RemoveChildRequest request, ServerCallContext context)
    {
        var command = new Commands.RemoveChildRequest
        {
            ChildTag = _tagMapper.MapFromDto(request.ChildTag), ParentTag = _tagMapper.MapFromDto(request.ParentTag)
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new RemoveChildReply { SuccessMessage = s },
            errorResponse => new RemoveChildReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        var tagBase = _tagMapper.MapFromDto(request.Tag);

        var taggableItem = request.ItemCase switch
        {
            TagItemRequest.ItemOneofCase.File => new TaggableFile { Path = request.File.Path } as TaggableItem,
            TagItemRequest.ItemOneofCase.Folder => new TaggableFolder { Path = request.Folder.Path },
            _ => throw new UnreachableException()
        };

        var command = new Commands.TagItemRequest { TaggableItem = taggableItem, Tag = tagBase };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply
            {
                TaggedItem = item switch
                {
                    TaggableFile file => new TaggedItem
                    {
                        File = new FileDto { Path = file.Path }, Tags = { file.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    TaggableFolder folder => new TaggedItem
                    {
                        Folder = new FolderDto { Path = folder.Path }, Tags = { folder.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    _ => throw new UnreachableException()
                }
            },
            errorResponse => new TagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        var tagBase = _tagMapper.MapFromDto(request.Tag);

        var taggableItem = request.ItemCase switch
        {
            UntagItemRequest.ItemOneofCase.File => new TaggableFile { Path = request.File.Path } as TaggableItem,
            UntagItemRequest.ItemOneofCase.Folder => new TaggableFolder { Path = request.Folder.Path },
            _ => throw new UnreachableException()
        };

        var command = new Commands.UntagItemRequest { Tag = tagBase, TaggableItem = taggableItem };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply
            {
                TaggedItem = item switch
                {
                    TaggableFile file => new TaggedItem
                    {
                        File = new FileDto { Path = file.Path }, Tags = { file.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    TaggableFolder folder => new TaggedItem
                    {
                        Folder = new FolderDto { Path = folder.Path }, Tags = { folder.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    _ => throw new UnreachableException()
                }
            },
            errorResponse => new UntagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var taggableItem = request.ItemCase switch
        {
            GetItemRequest.ItemOneofCase.File => new TaggableFile { Path = request.File.Path } as TaggableItem,
            GetItemRequest.ItemOneofCase.Folder => new TaggableFolder { Path = request.Folder.Path },
            _ => throw new UnreachableException()
        };

        var getItemQuery = new GetItemQuery { TaggableItem = taggableItem };

        var response = await _mediator.Send(getItemQuery, context.CancellationToken);

        return response.Match(
            taggedItem => new GetItemReply
            {
                TaggedItem = taggedItem switch
                {
                    TaggableFile file => new TaggedItem
                    {
                        File = new FileDto { Path = file.Path }, Tags = { file.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    TaggableFolder folder
                        => new TaggedItem
                        {
                            Folder = new FolderDto { Path = folder.Path }, Tags = { folder.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                        },
                    _ => throw new UnreachableException()
                }
            },
            errorResponse => new GetItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
    {
        var querySegments = request.QueryParams
            .Select(param => new TagQuerySegment { State = MapQuerySegmentState(param), Tag = _tagMapper.MapFromDto(param.Tag) })
            .ToArray();

        var getItemsByTagsQuery = new GetItemsByTagsQuery { QuerySegments = querySegments };

        var response = await _mediator.Send(getItemsByTagsQuery, context.CancellationToken);

        var results = response
            .Select(item
                => item switch
                {
                    TaggableFile file => new TaggedItem
                    {
                        File = new FileDto { Path = file.Path }, Tags = { file.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    TaggableFolder folder => new TaggedItem
                    {
                        Folder = new FolderDto { Path = folder.Path }, Tags = { folder.Tags.Select(@base => _tagMapper.MapToDto(@base)) }
                    },
                    _ => throw new UnreachableException()
                })
            .ToArray();

        return new GetItemsByTagsReply { TaggedItems = { results } };
    }

    public override async Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        var taggableItem = request.ItemCase switch
        {
            DoesItemExistsRequest.ItemOneofCase.Folder => new TaggableFile { Path = request.File.Path } as TaggableItem,
            DoesItemExistsRequest.ItemOneofCase.File => new TaggableFolder { Path = request.Folder.Path },
            _ => throw new UnreachableException()
        };

        var doesItemExistsQuery = new DoesItemExistsQuery { TaggableItem = taggableItem };

        var response = await _mediator.Send(doesItemExistsQuery, context.CancellationToken);

        return new DoesItemExistsReply { Exists = response };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        var tag = _tagMapper.MapFromDto(request.Tag);

        var doesTagExistsQuery = new GetTagQuery { TagBase = tag };

        var response = await _mediator.Send(doesTagExistsQuery, context.CancellationToken);

        return response is null
            ? new DoesTagExistsReply()
            : new DoesTagExistsReply { Tag = _tagMapper.MapToDto(tag) };
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

            var dto = _tagMapper.MapToDto(tag);
            var matchTagsReply = new SearchTagsReply
            {
                Tag = dto,
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
        if (command is null)
        {
            return new ErrorResponse($"Nothing to {undoOrRedo}.");
        }

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

        var response = await _mediator.Send(setTagNamingConventionCommand, context.CancellationToken);

        return response.Match(
            _ => new SetTagNamingConventionReply(),
            errorResponse => new SetTagNamingConventionReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<ExecuteLinkedActionReply> ExecuteLinkedAction(ExecuteLinkedActionRequest request, ServerCallContext context)
    {
        var taggableItem = request.ItemCase switch
        {
            ExecuteLinkedActionRequest.ItemOneofCase.File => new TaggableFile { Path = request.File.Path } as TaggableItem,
            ExecuteLinkedActionRequest.ItemOneofCase.Folder => new TaggableFolder { Path = request.Folder.Path },
            _ => throw new UnreachableException()
        };

        var command = new ExecuteLinkedRequest { Item = taggableItem };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            _ => new ExecuteLinkedActionReply(),
            errorResponse => new ExecuteLinkedActionReply { Error = new Error { Message = errorResponse.Message } });
    }

    private static Models.Options.NamingConvention Map(NamingConvention requestConvention)
        => requestConvention switch
        {
            NamingConvention.None => Models.Options.NamingConvention.Unchanged,
            NamingConvention.CamelCase => Models.Options.NamingConvention.CamelCase,
            NamingConvention.PascalCase => Models.Options.NamingConvention.PascalCase,
            NamingConvention.KebabCase => Models.Options.NamingConvention.KebabCase,
            NamingConvention.SnakeCase => Models.Options.NamingConvention.SnakeCase,
            _ => throw new ArgumentOutOfRangeException(nameof(requestConvention), requestConvention, null)
        };

    private static QuerySegmentState MapQuerySegmentState(GetItemsByTagsRequest.Types.TagQueryParam tagQueryParam)
        => tagQueryParam.State switch
        {
            GetItemsByTagsRequest.Types.QuerySegmentState.Exclude => QuerySegmentState.Exclude,
            GetItemsByTagsRequest.Types.QuerySegmentState.Include => QuerySegmentState.Include,
            GetItemsByTagsRequest.Types.QuerySegmentState.MustBePresent => QuerySegmentState.MustBePresent,
            _ => throw new UnreachableException()
        };
}
