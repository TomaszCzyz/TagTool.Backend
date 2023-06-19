using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class UntagItemRequest : ICommand<OneOf<TaggedItem, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required string ItemType { get; init; }

    public required string Identifier { get; init; }

    public IReversible GetReverse()
        => new TagItemRequest
        {
            Tag = Tag,
            ItemType = ItemType,
            Identifier = Identifier,
            TaggableItem = null
        };
}

[UsedImplicitly]
public class UntagItem : ICommandHandler<UntagItemRequest, OneOf<TaggedItem, ErrorResponse>>
{
    private readonly ILogger<UntagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public UntagItem(ILogger<UntagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggedItem, ErrorResponse>> Handle(UntagItemRequest request, CancellationToken cancellationToken)
    {
        var (tag, itemType, identifier) = (request.Tag, request.ItemType, request.Identifier);

        var existingItem = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier, cancellationToken);

        if (existingItem is null)
        {
            return new ErrorResponse($"There is no {request.ItemType} item {request.Identifier} in database.");
        }

        if (!existingItem.Tags.Contains(tag))
        {
            return new ErrorResponse($"{request.ItemType} item does not contain tag {request.Tag}.");
        }

        tag = await _dbContext.Tags.FirstAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, existingItem);
        var isRemoved = existingItem.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return isRemoved ? existingItem : new ErrorResponse($"Unable to remove tag {tag} from item {existingItem}.");
    }
}
