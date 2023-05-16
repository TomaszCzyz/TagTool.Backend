using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class TagItemRequest : ICommand<OneOf<TaggedItem, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required string ItemType { get; init; }

    public required string Identifier { get; init; }

    public IReversible GetReverse()
        => new UntagItemRequest
        {
            Tag = Tag,
            Identifier = Identifier,
            ItemType = ItemType
        };
}

[UsedImplicitly]
public class TagItem : ICommandHandler<TagItemRequest, OneOf<TaggedItem, ErrorResponse>>
{
    private readonly ILogger<TagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public TagItem(ILogger<TagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggedItem, ErrorResponse>> Handle(TagItemRequest request, CancellationToken cancellationToken)
    {
        var (tag, itemType, identifier) = (request.Tag, request.ItemType, request.Identifier);

        var existingTag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);
        tag = existingTag ?? (await _dbContext.Tags.AddAsync(tag, cancellationToken)).Entity;
        var existingItem = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier, cancellationToken);

        if (existingItem is not null)
        {
            if (existingItem.Tags.Contains(tag))
            {
                return new ErrorResponse($"Item {request.Identifier} already exists and it is tagged with a tag {tag}");
            }

            _logger.LogInformation("Tagging exiting item {@TaggedItem} with tag {@Tag}", existingItem, tag);
            existingItem.Tags.Add(tag);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return existingItem;
        }

        _logger.LogInformation("Tagging new item {@TaggedItem} with tag {@Tag}", existingItem, tag);
        var newTaggedItem = new TaggedItem
        {
            ItemType = itemType,
            UniqueIdentifier = identifier,
            Tags = new List<TagBase> { tag }
        };
        var entry = _dbContext.TaggedItems.Add(newTaggedItem);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entry.Entity;
    }
}
