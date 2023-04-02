using Ganss.Text;
using Grpc.Core;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using TaggedItem = TagTool.Backend.DomainTypes.TaggedItem;

namespace TagTool.Backend.Services;

public class TagService : Backend.TagService.TagServiceBase
{
    private readonly ILogger<TagService> _logger;
    private readonly IMediator _mediator;
    private readonly TagToolDbContext _dbContext;

    public TagService(ILogger<TagService> logger, IMediator mediator, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        var newTagName = request.TagName;
        var first = _dbContext.Tags.FirstOrDefault(tag => tag.Name == newTagName);

        if (first is not null)
        {
            return new CreateTagReply { ErrorMessage = $"Tag {newTagName} already exists." };
        }

        _logger.LogInformation("Creating new tag {@TagName}", newTagName);

        await _dbContext.Tags.AddAsync(new Tag { Name = newTagName });
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new CreateTagReply { CreatedTagName = newTagName };
    }

    public override async Task<DeleteTagReply> DeleteTag(DeleteTagRequest request, ServerCallContext context)
    {
        var newTagName = request.TagName;
        var existingTag = await _dbContext.Tags
            .Include(tag => tag.TaggedItems)
            .FirstOrDefaultAsync(tag => tag.Name == newTagName);

        if (existingTag is null)
        {
            return new DeleteTagReply { ErrorMessage = $"Tag {request.TagName} does not exists." };
        }

        if (!request.DeleteUsedToo && existingTag.TaggedItems.Count != 0)
        {
            var message = $"Tag {request.TagName} is in use and it was not deleted. " +
                          $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag.";

            return new DeleteTagReply { ErrorMessage = message };
        }

        _logger.LogInformation("Removing tag {@TagName} and all its occurrences in TaggedItems table", existingTag);
        _dbContext.Tags.Remove(existingTag);

        return new DeleteTagReply { DeletedTagName = request.TagName };
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        var command = new Commands.TagItemRequest
        {
            TagName = request.TagName,
            ItemType = request.Item.ItemType,
            Identifier = request.Item.Identifier
        };

        var response = await _mediator.Send(command);

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
        var queryResults = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .Where(item => item.Tags.Any(tag => request.TagNames.Contains(tag.Name)))
            .Select(item => new { Item = item, CommonTagsCount = item.Tags.Count(tag => request.TagNames.Contains(tag.Name)) })
            .OrderByDescending(arg => arg.CommonTagsCount)
            .Select(arg => arg.Item)
            .ToArrayAsync(context.CancellationToken);

        var results = queryResults.Select(item =>
            new TaggedItem
            {
                Item = new Item { ItemType = item.ItemType, Identifier = item.UniqueIdentifier },
                TagNames = { item.Tags.Select(tag => tag.Name) }
            });

        return new GetItemsByTagsReply { TaggedItem = { results } };
    }

    public override async Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);
        var existingItem = await _dbContext.TaggedItems
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        return new DoesItemExistsReply { Exists = existingItem is not null };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        var existingItem = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.Name == request.TagName);

        return new DoesTagExistsReply { Exists = existingItem is not null };
    }

    public override async Task SearchTags(
        SearchTagsRequest request,
        IServerStreamWriter<SearchTagsReply> responseStream,
        ServerCallContext context)
    {
        switch (request.SearchType)
        {
            case SearchTagsRequest.Types.SearchType.Wildcard:
                if (request.Name != "*") throw new NotImplementedException();

                await foreach (var tag in _dbContext.Tags)
                {
                    var matchTagsReply = new SearchTagsReply
                    {
                        TagName = tag.Name,
                        MatchedPart = { new SearchTagsReply.Types.MatchedPart { StartIndex = 0, Length = tag.Name.Length } },
                        IsExactMatch = true
                    };

                    await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
                }

                return;

            case SearchTagsRequest.Types.SearchType.StartsWith:
                var queryable = _dbContext.Tags
                    .Where(tag => tag.Name.StartsWith(request.Name))
                    .Select(tag => tag.Name)
                    .Take(request.ResultsLimit);

                foreach (var tagName in queryable)
                {
                    var matchedPart = new SearchTagsReply.Types.MatchedPart { StartIndex = 0, Length = tagName.IndexOf(request.Name.Last()) };
                    var matchTagsReply = new SearchTagsReply
                    {
                        TagName = tagName,
                        MatchedPart = { matchedPart },
                        IsExactMatch = matchedPart.Length == tagName.Length
                    };

                    await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
                }

                return;
            case SearchTagsRequest.Types.SearchType.Partial:
                await foreach (var tag in _dbContext.Tags)
                {
                    var tagName = tag.Name;
                    var ahoCorasick = new AhoCorasick(request.Name.Substrings().Distinct());

                    var matchedParts = ahoCorasick
                        .Search(tagName) // todo: safeguard for very long tagNames would be nice
                        .ExcludeOverlaying(tagName)
                        .Select(match => new SearchTagsReply.Types.MatchedPart { StartIndex = match.Index, Length = match.Word.Length })
                        .OrderByDescending(match => match.Length)
                        .ToList();

                    if (matchedParts.Count == 0) continue;

                    var matchTagsReply = new SearchTagsReply
                    {
                        TagName = tagName,
                        MatchedPart = { matchedParts },
                        IsExactMatch = matchedParts[0].Length == tagName.Length
                    };

                    await responseStream.WriteAsync(matchTagsReply, context.CancellationToken);
                }

                return;
        }
    }
}
