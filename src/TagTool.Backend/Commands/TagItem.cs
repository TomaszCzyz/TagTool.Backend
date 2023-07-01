using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

public class TagItemRequest : ICommand<OneOf<TaggableItem, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public required TaggableItem TaggableItem { get; init; }

    public IReversible GetReverse() => new UntagItemRequest { Tag = Tag, TaggableItem = TaggableItem };
}

[UsedImplicitly]
public class TagItem : ICommandHandler<TagItemRequest, OneOf<TaggableItem, ErrorResponse>>
{
    private readonly ILogger<TagItem> _logger;
    private readonly IImplicitTagsProvider _implicitTagsProvider;
    private readonly TagToolDbContext _dbContext;

    public TagItem(ILogger<TagItem> logger, IImplicitTagsProvider implicitTagsProvider, TagToolDbContext dbContext)
    {
        _logger = logger;
        _implicitTagsProvider = implicitTagsProvider;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggableItem, ErrorResponse>> Handle(TagItemRequest request, CancellationToken cancellationToken)
    {
        var taggableItem = await GetOrAddTaggableItem(request.TaggableItem, cancellationToken);
        var tag = await GetOrAddTag(request.Tag, cancellationToken);

        if (taggableItem.Tags.Contains(tag))
        {
            return new ErrorResponse($"Item {taggableItem} already contain tag {tag}");
        }

        _logger.LogInformation("Tagging item {@TaggedItem} with tag {@Tags}", taggableItem, tag);
        taggableItem.Tags.Add(tag);

        AddImplicitTags(taggableItem);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return taggableItem;
    }

    private void AddImplicitTags(TaggableItem taggableItem)
    {
        var implicitTags = _implicitTagsProvider.GetImplicitTags(taggableItem).ToArray();

        if (implicitTags.Length == 0) return;

        _logger.LogInformation("Tagging item {@TaggedItem} with implicit tags {@Tags}", taggableItem, implicitTags);
        foreach (var tag in implicitTags)
        {
            if (taggableItem.Tags.Select(@base => @base.FormattedName).Contains(tag.FormattedName)) continue;

            taggableItem.Tags.Add(tag);
        }
    }

    private async Task<TagBase> GetOrAddTag(TagBase tag, CancellationToken cancellationToken)
    {
        var existingTag = await _dbContext.Tags.FirstOrDefaultAsync(tagBase => tagBase.FormattedName == tag.FormattedName, cancellationToken);

        if (existingTag is not null)
        {
            return existingTag;
        }

        var entityEntry = await _dbContext.Tags.AddAsync(tag, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return entityEntry.Entity;
    }

    private async Task<TaggableItem> GetOrAddTaggableItem(TaggableItem taggableItem, CancellationToken cancellationToken)
    {
        TaggableItem? existingTaggableItem = taggableItem switch
        {
            TaggableFile taggableFile
                => await _dbContext.TaggableFiles
                    .Include(file => file.Tags)
                    .FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders
                    .Include(folder => folder.Tags)
                    .FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new UnreachableException()
        };

        if (existingTaggableItem is not null)
        {
            return existingTaggableItem;
        }

        taggableItem.Id = Guid.NewGuid();

        _logger.LogInformation("Creating new taggable item {@TaggableItem}", taggableItem);
        var entityEntry = await _dbContext.TaggedItems.AddAsync(taggableItem, cancellationToken);

        return entityEntry.Entity;
    }
}
