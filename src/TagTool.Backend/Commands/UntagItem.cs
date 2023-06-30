using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

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
        var (tag, taggableItem) = await FindExistingEntities(request.Tag, request.TaggableItem, cancellationToken);

        if (!AreEntitiesNotNull(tag, taggableItem, out var errorMessage))
        {
            return new ErrorResponse(errorMessage);
        }

        _logger.LogInformation("Removing tag {@Tag} from item {@TaggedItem}", tag, taggableItem);

        var isRemoved = taggableItem.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return isRemoved ? taggableItem : new ErrorResponse($"Unable to remove tag {tag} from item {taggableItem}.");
    }

    private async Task<(TagBase?, TaggableItem?)> FindExistingEntities(
        TagBase tag,
        TaggableItem taggableItem,
        CancellationToken cancellationToken)
    {
        var existingTag = _dbContext.Tags.FirstOrDefaultAsync(t => t.FormattedName == tag.FormattedName, cancellationToken);

        TaggableItem? existingTaggableItem = taggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles
                    .Include(file => file.Tags)
                    .FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders
                    .Include(folder => folder.Tags)
                    .FirstOrDefaultAsync(folder => folder.Path == taggableFolder.Path, cancellationToken),
            _ => throw new UnreachableException()
        };

        return (await existingTag, existingTaggableItem);
    }

    private static bool AreEntitiesNotNull(
        [NotNullWhen(true)] TagBase? tag,
        [NotNullWhen(true)] TaggableItem? taggableItem,
        [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = (tag, taggableItem) switch
        {
            (null, null) => $"There is no item {taggableItem} or tag {tag} in database.",
            (not null, null) => $"There is no item {taggableItem} in database.",
            (null, not null) => $"There is no item {taggableItem} in database.",
            _ => null
        };

        return errorMessage is null;
    }
}
