using System.Diagnostics;
using Grpc.Core;
using MediatR;
using OneOf.Types;
using TagTool.BackendNew.Commands;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Mappers;
using TagTool.BackendNew.Models;
using TagTool.BackendNew.Queries;
using TagTool.BackendNew.Services.Grpc.Dtos;
using DeleteTagRequest = TagTool.BackendNew.Services.Grpc.Dtos.DeleteTagRequest;
using Error = TagTool.BackendNew.Services.Grpc.Dtos.Error;
using TaggableItem = TagTool.BackendNew.Services.Grpc.Dtos.TaggableItem;

namespace TagTool.BackendNew.Services.Grpc;

public class TagsGrpcService : TagsService.TagsServiceBase
{
    private readonly ILogger<TagsGrpcService> _logger;
    private readonly IMediator _mediator;
    private readonly IOperationManger _operationManger;
    private readonly TaggableItemMapper _taggableItemMapper;

    public TagsGrpcService(
        ILogger<TagsGrpcService> logger,
        IMediator mediator,
        IOperationManger operationManger,
        TaggableItemMapper taggableItemMapper)
    {
        _logger = logger;
        _mediator = mediator;
        _operationManger = operationManger;
        _taggableItemMapper = taggableItemMapper;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Text);

        var command = new CreateTag
        {
            Text = request.Text
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new CreateTagReply
            {
                Tag = new Tag
                {
                    Id = tagBase.Id, Text = tagBase.Text
                }
            },
            error => new CreateTagReply
            {
                ErrorMessage = error.Value
            });
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
                await responseStream.WriteAsync(new CanCreateTagReply
                {
                    Error = new Error
                    {
                        Message = "Tag name cannot be empty."
                    }
                });
                continue;
            }

            var query = new CanCreateTag
            {
                NewTagText = request.TagName
            };

            var response = await _mediator.Send(query, context.CancellationToken);

            var reply = response.Match(
                _ => new CanCreateTagReply(),
                error => new CanCreateTagReply
                {
                    Error = new Error
                    {
                        Message = error.Value
                    }
                });

            await responseStream.WriteAsync(reply);
        }
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var command = new Commands.DeleteTagRequest
        {
            Id = request.TagId, DeleteUsedToo = request.DeleteUsedToo
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            tagBase => new DeleteTagReply
            {
                Tag = tagBase.ToDto()
            },
            error => new DeleteTagReply
            {
                ErrorMessage = error.Value
            });
    }

    public override async Task<AddItemReply> AddItem(AddItemRequest request, ServerCallContext context)
    {
        var addItem = new AddItem
        {
            ItemType = request.Item.Type, ItemArgs = request.Item.Payload
        };

        var response = await _mediator.Send(addItem, context.CancellationToken);
        return response.Match(
            item => new AddItemReply
            {
                TaggableItem = new TaggableItem
                {
                    Item = Map(item),
                    Tags =
                    {
                        item.Tags.ToDtos()
                    }
                }
            },
            error => new AddItemReply
            {
                ErrorMessage = error.Value
            });
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.ItemId);

        var command = new TagItem
        {
            ItemId = new Guid(request.ItemId), TagId = request.TagId
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new TagItemReply
            {
                TaggableItem = MapTaggableItem(item)
            },
            error => new TagItemReply
            {
                ErrorMessage = error.Value
            });
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.ItemId);

        var command = new UntagItem
        {
            ItemId = new Guid(request.ItemId), TagId = request.TagId
        };

        var response = await _mediator.Send(command, context.CancellationToken);

        return response.Match(
            item => new UntagItemReply
            {
                TaggableItem = MapTaggableItem(item)
            },
            error => new UntagItemReply
            {
                ErrorMessage = error.Value
            });
    }

    public override Task<GetOperationsReply> GetOperations(GetOperationsRequest request, ServerCallContext context)
    {
        var result = _operationManger.GetOperationNames();

        var operations = result.Select(tuple
            => new GetOperationsReply.Types.ItemTypeOperations
            {
                TypeName = tuple.TypeName,
                Name =
                {
                    tuple.OperationNames
                }
            });
        var reply = new GetOperationsReply
        {
            Operations =
            {
                operations
            }
        };

        return Task.FromResult(reply);
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
        var query = new GetItemById
        {
            Id = new Guid(request.ItemId)
        };
        var response = await _mediator.Send(query, context.CancellationToken);

        return response.Match(
            item => new GetItemReply
            {
                TaggableItem = MapTaggableItem(item)
            },
            _ => new GetItemReply
            {
                ErrorMessage = $"Could not find taggable item with id {request.ItemId}."
            });
    }


    public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.QueryParams);

        // if (request.QueryParams.Any(param => param.Tag is null))
        // {
        //     throw new ArgumentNullException(nameof(request), "No tag in a tag query can be null");
        // }

        // todo: add validation - tag cannot be null
        var querySegments = request.QueryParams
            .Select(param => new Models.TagQueryParam
            {
                State = param.State.MapFromDto(), TagId = param.TagId
            })
            .ToArray();

        var query = new GetItemsByTagsQuery
        {
            QuerySegments = querySegments
        };

        var response = await _mediator.Send(query, context.CancellationToken);

        return new GetItemsByTagsReply
        {
            TaggedItems =
            {
                response.Select(MapTaggableItem)
            }
        };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request.Text);

        var doesTagExistsQuery = new GetTagByText
        {
            Text = request.Text
        };

        var response = await _mediator.Send(doesTagExistsQuery, context.CancellationToken);

        return response.Match(
            tag => new DoesTagExistsReply
            {
                Tag = tag.ToDto()
            },
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
            SearchTagsRequest.Types.SearchType.Wildcard => new SearchTagsWildcardRequest
            {
                Value = value, ResultsLimit = limit
            },
            SearchTagsRequest.Types.SearchType.StartsWith => new SearchTagsStartsWithRequest
            {
                Value = value, ResultsLimit = limit
            },
            SearchTagsRequest.Types.SearchType.Fuzzy => new SearchTagsFuzzyRequest
            {
                Value = value, ResultsLimit = limit
            },
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        await foreach (var (tag, parts) in _mediator.CreateStream(query, context.CancellationToken))
        {
            var matchedParts = parts
                .Select(part => new SearchTagsReply.Types.MatchedPart
                {
                    StartIndex = part.StartIndex, Length = part.Length
                })
                .ToArray();

            var matchTagsReply = new SearchTagsReply
            {
                Tag = tag.ToDto(),
                MatchedPart =
                {
                    matchedParts
                },
                IsExactMatch = matchedParts[0].Length == tag.Text.Length
            };

            await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
        }
    }

    private Item Map(Entities.TaggableItem item)
    {
        var (type, payload) = _taggableItemMapper.MapFromObj(item);
        return new Item
        {
            Type = type, Payload = payload
        };
    }

    private TaggableItem MapTaggableItem(Entities.TaggableItem item)
        => new()
        {
            Item = Map(item),
            Tags =
            {
                item.Tags.ToDtos()
            }
        };
}
