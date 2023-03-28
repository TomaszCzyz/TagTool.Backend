using Ganss.Text;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using TagTool.Backend.New;
using TagTool.Backend.New.DomainTypes;
using TagTool.Backend.New.ItemsActions;
using TaggedItem = TagTool.Backend.New.DomainTypes.TaggedItem;

namespace TagTool.Backend.Services;

public class NewTagService : New.NewTagService.NewTagServiceBase
{
    private readonly ILogger<NewTagService> _logger;

    public NewTagService(ILogger<NewTagService> logger)
    {
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

    public override async Task<DoesItemExistsReply> DoesItemExists(DoesItemExistsRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);
        var existingItem = await db.TaggedItems.FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        return new DoesItemExistsReply { Exists = existingItem is not null };
    }

    public override async Task<DoesTagExistsReply> DoesTagExists(DoesTagExistsRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();

        var existingItem = await db.Tags.FirstOrDefaultAsync(tag => tag.Name == request.TagName);

        return new DoesTagExistsReply { Exists = existingItem is not null };
    }

    public override async Task SearchTags(
        SearchTagsRequest request,
        IServerStreamWriter<SearchTagsReply> responseStream,
        ServerCallContext context)
    {
        var dict = request.Name.Substrings().Distinct();
        var ahoCorasick = new AhoCorasick(dict);

        await using var db = new TagContext();

        switch (request.SearchType)
        {
            case SearchTagsRequest.Types.SearchType.StartsWith:
                var queryable = db.Tags.Where(tag => tag.Name.StartsWith(request.Name)).Select(tag => tag.Name).Take(request.ResultsLimit);
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
                await foreach (var tag in db.Tags)
                {
                    var tagName = tag.Name;

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

    public override async Task<InvokeItemActionReply> InvokeAction(InvokeItemActionRequest request, ServerCallContext context)
    {
        await using var db = new TagContext();
        var (itemType, identifier) = (request.Item.ItemType, request.Item.Identifier);

        var taggedItem = await db.TaggedItems.FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier);

        if (taggedItem is null)
        {
            return new InvokeItemActionReply { ErrorMessage = "Requested item does not exists in database." };
        }

        var actionMessage = request.Action.Unpack(TypeRegistry.FromMessages(ItemsActionsMessagesReflection.Descriptor.MessageTypes));

        if (actionMessage is null)
        {
            return new InvokeItemActionReply { ErrorMessage = "Could not match any action message." };
        }

        switch (actionMessage)
        {
            case MoveAction:
                Console.WriteLine("Move Action!!!");
                break;
            default:
                Console.WriteLine("Unknown action!!!");
                break;
        }

        return new InvokeItemActionReply { SuccessMessage = "" };
    }
}
