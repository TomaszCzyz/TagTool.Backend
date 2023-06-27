using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class UntagItemRequest : ICommand<OneOf<TaggableItem, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required TaggableItem TaggableItem { get; init; }

    public IReversible GetReverse() => new TagItemRequest { Tag = Tag, TaggableItem = TaggableItem };
}

[UsedImplicitly]
public class UntagItem : ICommandHandler<UntagItemRequest, OneOf<TaggableItem, ErrorResponse>>
{
    private readonly ILogger<UntagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public UntagItem(ILogger<UntagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggableItem, ErrorResponse>> Handle(UntagItemRequest request, CancellationToken cancellationToken)
    {
        var tag = request.Tag;

        TaggableItem? taggableItem = request.TaggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders.FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (taggableItem is null)
        {
            return new ErrorResponse($"There is no {request.TaggableItem} in database.");
        }

        var existingItem = await _dbContext.TaggedItems
            .Include(item => item.Tags)
            .FirstAsync(item => item.Id == taggableItem.Id, cancellationToken);

        // todo: get rid off ".Select(@base => @base.FormattedName)" by overloading equals or adding comparer
        if (!existingItem.Tags.Select(@base => @base.FormattedName).Contains(tag.FormattedName))
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
