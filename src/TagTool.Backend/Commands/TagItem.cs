using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class TagItemRequest : ICommand<OneOf<TaggedItemBase, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required TaggableItem TaggableItem { get; init; }

    public IReversible GetReverse() => new UntagItemRequest { Tag = Tag, TaggableItem = TaggableItem };
}

[UsedImplicitly]
public class TagItem : ICommandHandler<TagItemRequest, OneOf<TaggedItemBase, ErrorResponse>>
{
    private readonly ILogger<TagItem> _logger;
    private readonly TagToolDbContext _dbContext;

    public TagItem(ILogger<TagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggedItemBase, ErrorResponse>> Handle(TagItemRequest request, CancellationToken cancellationToken)
    {
        var tag = request.Tag;

        var existingTag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);
        tag = existingTag ?? (await _dbContext.Tags.AddAsync(tag, cancellationToken)).Entity;

        TaggableItem? taggableItem = request.TaggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders.FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

        if (taggableItem is not null)
        {
            var existingItem = await _dbContext.TaggedItemsBase
                .Include(item => item.Item)
                .Include(item => item.Tags)
                .FirstAsync(item => item.Item.Id == request.TaggableItem.Id, cancellationToken);

            if (existingItem.Tags.Contains(tag))
            {
                return new ErrorResponse($"Item {request.TaggableItem} already exists and it is tagged with a tag {tag}");
            }

            _logger.LogInformation("Tagging exiting item {@TaggedItem} with tag {@Tag}", existingItem, tag);
            existingItem.Tags.Add(tag);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return existingItem;
        }

        _logger.LogInformation("Tagging new item {@TaggedItem} with tag {@Tag}", taggableItem, tag);
        request.TaggableItem.Id = Guid.NewGuid();
        var entry = _dbContext.TaggedItemsBase.Add(new TaggedItemBase { Item = request.TaggableItem, Tags = new List<TagBase> { tag } });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }
}
