using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class UntagItemResponse
{
    public TaggedItem? TaggedItem { get; init; }

    public string? ErrorMessage { get; init; }
}

public class UntagItemRequest : IRequest<UntagItemResponse>
{
    public required string TagName { get; init; }

    public required string ItemType { get; init; }

    public required string Identifier { get; init; }
}

[UsedImplicitly]
public class UntagItem : IRequestHandler<UntagItemRequest, UntagItemResponse>
{
    private readonly ILogger<UntagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public UntagItem(ILogger<UntagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<UntagItemResponse> Handle(UntagItemRequest request, CancellationToken cancellationToken)
    {
        var (tagName, itemType, identifier) = (request.TagName, request.ItemType, request.Identifier);

        var existingItem = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.ItemType == itemType && item.UniqueIdentifier == identifier, cancellationToken);

        if (existingItem is null)
        {
            return new UntagItemResponse { ErrorMessage = $"There is no {request.ItemType} item {request.Identifier} in database." };
        }

        if (!existingItem.Tags.Select(tag => tag.Name).Contains(tagName))
        {
            return new UntagItemResponse { ErrorMessage = $"{request.ItemType} item does not contain tag {request.TagName}." };
        }

        var tag = await _dbContext.Tags.FirstAsync(tag => tag.Name == tagName, cancellationToken);

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, existingItem);
        var isRemoved = existingItem.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return !isRemoved
            ? new UntagItemResponse { ErrorMessage = $"Unable to remove tag {tag} from item {existingItem}." }
            : new UntagItemResponse { TaggedItem = existingItem };
    }
}
