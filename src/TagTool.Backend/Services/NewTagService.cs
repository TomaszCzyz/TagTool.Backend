using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.New;
using TagTool.Backend.Repositories;
using TaggedItem = TagTool.Backend.New.TaggedItem;

namespace TagTool.Backend.Services;

public class NewTagService : New.NewTagService.NewTagServiceBase
{
    private readonly ITagsRepo _tagsRepo;
    private readonly ITaggedItemsRepo _taggedItemsRepo;
    private readonly ITaggersManager _taggersManager;
    private readonly ILogger<NewTagService> _logger;

    public NewTagService(
        ITagsRepo tagsRepo,
        ITaggedItemsRepo taggedItemsRepo,
        ITaggersManager taggersManager,
        ILogger<NewTagService> logger)
    {
        _tagsRepo = tagsRepo;
        _taggedItemsRepo = taggedItemsRepo;
        _taggersManager = taggersManager;
        _logger = logger;
    }

    public override async Task<CreateTagReply> CreateTag(CreateTagRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var newTagName = request.TagName;
        var first = db.Tags.FirstOrDefault(tag => tag.Name == newTagName);

        if (first is not null)
        {
            return new CreateTagReply { ErrorMessage = $"Tag {newTagName} already exists." };
        }

        _logger.LogInformation("Creating new tag {@TagName}", newTagName);

        db.Tags.Add(new Tag { Name = newTagName });

        await db.SaveChangesAsync(context.CancellationToken);

        return new CreateTagReply { CreatedTagName = newTagName };
    }

    public override async Task<New.DeleteTagReply> DeleteTag(New.DeleteTagRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var newTagName = request.TagName;
        var existingTag = db.Tags
            .Include(tag => tag.TaggedItems)
            .FirstOrDefault(tag => tag.Name == newTagName);

        if (existingTag is null)
        {
            return new New.DeleteTagReply { ErrorMessage = $"Tag {request.TagName} does not exists." };
        }

        if (!request.DeleteUsedToo && existingTag.TaggedItems.Count != 0)
        {
            var message = $"Tag {request.TagName} is in use and it was not deleted. " +
                          $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag.";

            return new New.DeleteTagReply { ErrorMessage = message };
        }

        _logger.LogInformation("Removing tag {@TagName} and all its occurrences in TaggedItems table", existingTag);
        db.Tags.Remove(existingTag);

        return new New.DeleteTagReply { DeletedTagName = request.TagName };
    }

    public override async Task<TagItemReply> TagItem(TagItemRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();
        var (tagName, itemType, identifier) = (request.TagName, request.Item.ItemType, request.Item.Identifier);

        var existingTag = await db.Tags.FirstOrDefaultAsync(tag => tag.Name == tagName);
        var tag = existingTag ?? db.Tags.Add(new Tag { Name = tagName }).Entity;
        var existingItem = await db.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        if (existingItem is not null)
        {
            if (existingItem.Tags.Contains(tag))
            {
                return new TagItemReply { ErrorMessage = $"Item {request.Item} already exists and it is tagged with a tag {tagName}" };
            }

            _logger.LogInformation("Tagging exiting item {@TaggedItem} with tag {@Tag}", existingItem, tag);
            existingItem.Tags.Add(tag);

            return new TagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { new[] { tag.Name } } } };
        }

        _logger.LogInformation("Tagging new item {@TaggedItem} with tag {Tag}", existingItem, tagName);
        db.TaggedItems.Add(
            new Models.TaggedItem
            {
                ItemType = itemType,
                UniqueIdentifier = identifier,
                Tags = new List<Tag> { tag }
            });

        await db.SaveChangesAsync(context.CancellationToken);

        return new TagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { new[] { tag.Name } } } };
    }

    public override async Task<UntagItemReply> UntagItem(UntagItemRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();
        var (tagName, itemType, identifier) = (request.TagName, request.Item.ItemType, request.Item.Identifier);

        var existingItem = await db.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        if (existingItem is null)
        {
            return new UntagItemReply { ErrorMessage = $"There is no item {request.Item} in database." };
        }

        if (!existingItem.Tags.Select(tag => tag.Name).Contains(tagName))
        {
            return new UntagItemReply { ErrorMessage = $"There is no item {request.Item} in database." };
        }

        var tag = await db.Tags.FirstAsync(tag => tag.Name == tagName);

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, existingItem);
        var isRemoved = existingItem.Tags.Remove(tag);

        return !isRemoved
            ? new UntagItemReply { ErrorMessage = $"Unable to remove tag {tag} from item {existingItem}." }
            : new UntagItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { existingItem.Tags.Select(t => t.Name) } } };
    }

    public override async Task<GetItemReply> GetItem(GetItemRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);
        var existingItem = await db.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        return existingItem is null
            ? new GetItemReply { ErrorMessage = $"Requested item {request.Item} does not exists." }
            : new GetItemReply { TaggedItem = new TaggedItem { Item = request.Item, TagNames = { existingItem.Tags.Select(tag => tag.Name) } } };
    }

    public override async Task<GetItemsByTagsReply> GetItemsByTags(GetItemsByTagsRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var queryable = db.Tags
            .Where(tag => request.TagNames.Contains(tag.Name))
            .SelectMany(tag => tag.TaggedItems)
            .Select(item => new { Item = item, CommonTagsCount = item.Tags.Select(tag => tag.Name).Intersect(request.TagNames).Count() })
            .OrderByDescending(arg => arg.CommonTagsCount);

        var results = queryable.Select(arg =>
            new TaggedItem
            {
                Item = new Item { ItemType = arg.Item.ItemType, Identifier = arg.Item.UniqueIdentifier },
                TagNames = { arg.Item.Tags.Select(tag => tag.Name) }
            });

        return new GetItemsByTagsReply { TaggedItem = { results } };
    }

    public override Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        return base.DoesItemExists(request, context);
    }

    public override Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        return base.DoesTagExists(request, context);
    }

    public override Task SearchTags(SearchTagsRequest request, IServerStreamWriter<SearchTagsReply> responseStream, ServerCallContext context)
    {
        return base.SearchTags(request, responseStream, context);
    }
}
