using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class UntagItemRequest : ICommand<OneOf<TaggedItemBase, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required TaggableItem TaggableItem { get; init; }

    public IReversible GetReverse() => new TagItemRequest { Tag = Tag, TaggableItem = TaggableItem };
}

[UsedImplicitly]
public class UntagItem : ICommandHandler<UntagItemRequest, OneOf<TaggedItemBase, ErrorResponse>>
{
    private readonly ILogger<UntagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public UntagItem(ILogger<UntagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggedItemBase, ErrorResponse>> Handle(UntagItemRequest request, CancellationToken cancellationToken)
    {
        var tag = request.Tag;

        var existingItem = await _dbContext.TaggedItemsBase
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.Item == request.TaggableItem, cancellationToken);

        if (existingItem is null)
        {
            return new ErrorResponse($"There is no {request.TaggableItem} in database.");
        }

        if (!existingItem.Tags.Contains(tag))
        {
            return new ErrorResponse($"{request.TaggableItem} item does not contain tag {request.Tag}.");
        }

        tag = await _dbContext.Tags.FirstAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, existingItem);
        var isRemoved = existingItem.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return isRemoved ? existingItem : new ErrorResponse($"Unable to remove tag {tag} from item {existingItem}.");
    }
}
