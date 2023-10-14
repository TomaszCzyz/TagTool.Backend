using System.Diagnostics;
using Grpc.Core;
using Hangfire;
using Hangfire.Storage;
using MediatR;
using OneOf;
using TagTool.Backend.Actions;
using TagTool.Backend.Commands;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Extensions;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Queries;

namespace TagTool.Backend.Services.Grpc;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ILogger<TagService> _logger;
    private readonly IMediator _mediator;
    private readonly ITagMapper _tagMapper;
    private readonly ITaggableItemMapper _taggableItemMapper;

    // todo: move functionalities associated with below fields to command handlers 
    private readonly ICommandsHistory _commandsHistory;

    public TagService(
        ILogger<TagService> logger,
        IMediator mediator,
        ICommandsHistory commandsHistory,
        ITagMapper tagMapper,
        ITaggableItemMapper taggableItemMapper)
    {
        _logger = logger;
        _mediator = mediator;
        _commandsHistory = commandsHistory;
        _tagMapper = tagMapper;
        _taggableItemMapper = taggableItemMapper;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Tag);

        var tag = _tagMapper.MapFromDto(request.Tag);

        var command = new Commands.CreateTagRequest { Tag = tag };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new CreateTagReply { Tag = _tagMapper.MapToDto(tagBase) },
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
        ArgumentNullException.ThrowIfNull(request.Tag);

        var tag = _tagMapper.MapFromDto(request.Tag);

        var command = new Commands.DeleteTagRequest { Tag = tag };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new DeleteTagReply { Tag = _tagMapper.MapToDto(tagBase) },
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
        ArgumentException.ThrowIfNullOrEmpty(request.GroupName);
        ArgumentNullException.ThrowIfNull(request.Tag);

        var command = new Commands.AddSynonymRequest { GroupName = request.GroupName, Tag = _tagMapper.MapFromDto(request.Tag) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new AddSynonymReply { SuccessMessage = s },
            errorResponse => new AddSynonymReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<RemoveSynonymReply> RemoveSynonym(RemoveSynonymRequest request, ServerCallContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(request.GroupName);
        ArgumentNullException.ThrowIfNull(request.Tag);

        var command = new Commands.RemoveSynonymRequest { GroupName = request.GroupName, Tag = _tagMapper.MapFromDto(request.Tag) };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            s => new RemoveSynonymReply { SuccessMessage = s },
            errorResponse => new RemoveSynonymReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<AddChildReply> AddChild(AddChildRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.ChildTag);
        ArgumentNullException.ThrowIfNull(request.ParentTag);

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
        ArgumentNullException.ThrowIfNull(request.ChildTag);
        ArgumentNullException.ThrowIfNull(request.ParentTag);

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
        ArgumentNullException.ThrowIfNull(request.Item);
        ArgumentNullException.ThrowIfNull(request.Tag);

        var tagBase = _tagMapper.MapFromDto(request.Tag);
        var taggableItem = _taggableItemMapper.MapFromDto(request.Item);

        var command = new Commands.TagItemRequest { TaggableItem = taggableItem, Tag = tagBase };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply { Item = MapItem(item) },
            errorResponse => new TagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Item);
        ArgumentNullException.ThrowIfNull(request.Tag);

        var tagBase = _tagMapper.MapFromDto(request.Tag);
        var taggableItem = _taggableItemMapper.MapFromDto(request.Item);

        var command = new Commands.UntagItemRequest { Tag = tagBase, TaggableItem = taggableItem };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply { TaggedItem = MapItem(item) },
            errorResponse => new UntagItemReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        if (request.Id is not null && Guid.TryParse(request.Id.AsSpan(), out var guid))
        {
            var query = new GetItemByIdQuery { Id = guid };
            var item = await _mediator.Send(query, context.CancellationToken);

            return item is not null
                ? new GetItemReply { TaggedItem = MapItem(item) }
                : new GetItemReply { ErrorMessage = $"Could not find taggable item with id {request.Id}." };
        }

        ArgumentNullException.ThrowIfNull(request.TaggableItemDto);

        var getItemQuery = new GetItemQuery { TaggableItem = _taggableItemMapper.MapFromDto(request.TaggableItemDto) };
        var response = await _mediator.Send(getItemQuery, context.CancellationToken);

        return response is not null
            ? new GetItemReply { TaggedItem = MapItem(response) }
            : new GetItemReply { ErrorMessage = $"Could not find taggable item {request.TaggableItemDto} in a database." };
    }

    public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.QueryParams);

        if (request.QueryParams.Any(param => param.Tag is null))
        {
            throw new ArgumentNullException(nameof(request), "No tag in a tag query can be null");
        }

        // todo: add validation - tag cannot be null
        var querySegments = request.QueryParams
            .Select(param => new TagQuerySegment { State = MapQuerySegmentStateFromDto(param.State), Tag = _tagMapper.MapFromDto(param.Tag) })
            .ToArray();

        var query = new GetItemsByTagsQuery { QuerySegments = querySegments };

        var response = await _mediator.Send(query, context.CancellationToken);

        return new GetItemsByTagsReply { TaggedItems = { response.Select(MapItem) } };
    }

    public override async Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Item);

        var taggableItem = _taggableItemMapper.MapFromDto(request.Item);

        var doesItemExistsQuery = new DoesItemExistsQuery { TaggableItem = taggableItem };

        var response = await _mediator.Send(doesItemExistsQuery, context.CancellationToken);

        return new DoesItemExistsReply { Exists = response };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Tag);

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

        var result = await InvokeCommand(nameof(Undo), undoCommand, context.CancellationToken);

        return result.Match(
            s => new UndoReply { UndoCommand = s },
            errorResponse => new UndoReply { ErrorMessage = errorResponse.Message });
    }

    public override async Task<RedoReply> Redo(RedoRequest request, ServerCallContext context)
    {
        var redoCommand = _commandsHistory.GetRedoCommand();

        var result = await InvokeCommand(nameof(Redo), redoCommand, context.CancellationToken);

        return result.Match(
            s => new RedoReply { RedoCommand = s },
            errorResponse => new RedoReply { ErrorMessage = errorResponse.Message });
    }

    private async Task<OneOf<string, ErrorResponse>> InvokeCommand(
        string undoOrRedo,
        IReversible? command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return new ErrorResponse($"Nothing to {undoOrRedo}.");
        }

        var response = await _mediator.Send(command, cancellationToken);

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
        ArgumentNullException.ThrowIfNull(request.Item);

        var taggableItem = _taggableItemMapper.MapFromDto(request.Item);

        var command = new ExecuteLinkedRequest { Item = taggableItem };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            _ => new ExecuteLinkedActionReply(),
            errorResponse => new ExecuteLinkedActionReply { Error = new Error { Message = errorResponse.Message } });
    }

    public override async Task<AddOrUpdateTaskReply> AddOrUpdateTask(AddOrUpdateTaskRequest request, ServerCallContext context)
    {
        var tagQuery = new TagQuery { QuerySegments = request.QueryParams.Select(MapTagQuerySegmentFromDto).ToArray() };
        var actionAttributes = request.ActionAttributes.Values.ToDictionary(pair => pair.Key, pair => pair.Value);

        var command = new Commands.AddOrUpdateTaskRequest
        {
            TaskId = request.TaskId,
            TagQuery = tagQuery,
            ActionId = request.ActionId,
            ActionAttributes = actionAttributes,
            Triggers = request.Triggers.Select(info => new Trigger { Arg = info.Arg, Type = MapTriggerType(info.Type) }).ToArray()
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        // todo: extend reply with error info e.t.c.
        return response.Match(_ => new AddOrUpdateTaskReply(), _ => new AddOrUpdateTaskReply());

        Models.TriggerType MapTriggerType(TriggerType triggerTypeDto)
            => triggerTypeDto switch
            {
                TriggerType.Manual => Models.TriggerType.Manual,
                TriggerType.Cron => Models.TriggerType.Cron,
                TriggerType.Event => Models.TriggerType.Event,
                _ => throw new ArgumentOutOfRangeException(nameof(triggerTypeDto), triggerTypeDto, null)
            };
    }

    public override async Task<GetAvailableActionsReply> GetAvailableActions(GetAvailableActionsRequest request, ServerCallContext context)
    {
        var query = new GetAvailableActionsQuery();

        var response = await _mediator.Send(query, context.CancellationToken);

        var jobInfos = response.Select(info =>
            new ActionInfo
            {
                Id = info.Id,
                Description = info.Description,
                AttributesDescriptions = new Attributes { Values = { info.AttributesDescriptions } },
                ApplicableToItemTypes = { info.ItemTypes.Select(tag => _tagMapper.MapToDto(tag)) }
            });

        return new GetAvailableActionsReply { Infos = { jobInfos } };
    }

    public override async Task GetExistingTasks(
        GetExistingTasksRequest request,
        IServerStreamWriter<GetExistingTasksReply> responseStream,
        ServerCallContext context)
    {
        using var connection = JobStorage.Current.GetConnection();
        var recurringJobs = connection.GetRecurringJobs();

        foreach (var recurringJob in recurringJobs)
        {
            var jobArgs = recurringJob.Job.Args;
            if (jobArgs.Count == 2 && jobArgs[0] is TagQuery tagQuery && jobArgs[1] is Dictionary<string, string> data)
            {
                var tagQueryParams = tagQuery.QuerySegments.Select(MapTagQuerySegmentToDto);

                if (!recurringJob.Job.Type.IsAssignableTo(typeof(IAction)))
                {
                    _logger.LogWarning("Recurring job with unknown job type {@RecurringJobDto}", recurringJob);
                    return;
                }

                // todo: Rework this, because it hurts me eyes.
                var instanceId = (Activator.CreateInstance(recurringJob.Job.Type) as IAction)!.Id;

                // todo: add event triggers and decide what to do with multiple schedules of the same task (probably ban it)
                var reply = new GetExistingTasksReply
                {
                    TaskId = recurringJob.Id,
                    QueryParams = { tagQueryParams },
                    ActionId = instanceId,
                    ActionAttributes = new Attributes { Values = { data } },
                    Triggers = { new TriggerInfo { Type = TriggerType.Cron, Arg = recurringJob.Cron } }
                };

                await responseStream.WriteAsync(reply);
            }
            else
            {
                _logger.LogWarning("Recurring job with unknown arguments {@RecurringJobDto}", recurringJob);
            }
        }
    }

    private TaggedItem MapItem(TaggableItem i) => new() { TaggableItem = _taggableItemMapper.MapToDto(i), Tags = { _tagMapper.MapToDtos(i.Tags) } };

    private TagQueryParam MapTagQuerySegmentToDto(TagQuerySegment segment)
        => new() { Tag = _tagMapper.MapToDto(segment.Tag), State = MapQuerySegmentStateToDto(segment.State) };

    private TagQuerySegment MapTagQuerySegmentFromDto(TagQueryParam segment)
        => new() { Tag = _tagMapper.MapFromDto(segment.Tag), State = MapQuerySegmentStateFromDto(segment.State) };

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

    private static QuerySegmentState MapQuerySegmentStateFromDto(TagQueryParam.Types.QuerySegmentState state)
        => state switch
        {
            TagQueryParam.Types.QuerySegmentState.Exclude => QuerySegmentState.Exclude,
            TagQueryParam.Types.QuerySegmentState.Include => QuerySegmentState.Include,
            TagQueryParam.Types.QuerySegmentState.MustBePresent => QuerySegmentState.MustBePresent,
            _ => throw new UnreachableException()
        };

    private static TagQueryParam.Types.QuerySegmentState MapQuerySegmentStateToDto(QuerySegmentState querySegment)
        => querySegment switch
        {
            QuerySegmentState.Exclude => TagQueryParam.Types.QuerySegmentState.Exclude,
            QuerySegmentState.Include => TagQueryParam.Types.QuerySegmentState.Include,
            QuerySegmentState.MustBePresent => TagQueryParam.Types.QuerySegmentState.MustBePresent,
            _ => throw new UnreachableException()
        };
}
