﻿using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

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
    private readonly TagToolDbContext _dbContext;

    public TagItem(ILogger<TagItem> logger, TagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TaggableItem, ErrorResponse>> Handle(TagItemRequest request, CancellationToken cancellationToken)
    {
        var (tag, taggableItem) = await FindExistingEntities(request.Tag, request.TaggableItem, cancellationToken);

        switch ((tag, taggableItem))
        {
            case (null, null):
                _logger.LogInformation("Tagging new item {@TaggedItem} with new tag {@Tag}", taggableItem, request.Tag);
                request.TaggableItem.Id = Guid.NewGuid();
                request.TaggableItem.Tags.Add(request.Tag);

                await _dbContext.Tags.AddAsync(request.Tag, cancellationToken);
                await _dbContext.TaggedItems.AddAsync(request.TaggableItem, cancellationToken);
                break;
            case (not null, null):
                _logger.LogInformation("Tagging new item {@TaggedItem} with tag {@Tag}", taggableItem, request.Tag);
                request.TaggableItem.Id = Guid.NewGuid();
                request.TaggableItem.Tags.Add(tag);

                await _dbContext.TaggedItems.AddAsync(request.TaggableItem, cancellationToken);
                break;
            case (null, not null):
                _logger.LogInformation("Tagging item {@TaggedItem} with new tag {@Tag}", taggableItem, request.Tag);
                taggableItem.Tags.Add(request.Tag);

                await _dbContext.Tags.AddAsync(request.Tag, cancellationToken);
                _dbContext.TaggedItems.Update(taggableItem);
                break;
            case (not null, not null):
                if (taggableItem.Tags.Contains(tag))
                {
                    _logger.LogInformation("Existing item {@TaggedItem} is already tagged with tag {@Tag}", taggableItem, request.Tag);
                    return new ErrorResponse($"Item {request.TaggableItem} already exists and it is tagged with a tag {request.Tag}");
                }

                _logger.LogInformation("Tagging item {@TaggedItem} with tag {@Tag}", taggableItem, tag);
                taggableItem.Tags.Add(tag);
                break;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return request.TaggableItem;
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
                => await _dbContext.TaggableFiles.FirstOrDefaultAsync(file => file.Path == taggableFile.Path, cancellationToken),
            TaggableFolder taggableFolder
                => await _dbContext.TaggableFolders.FirstOrDefaultAsync(file => file.Path == taggableFolder.Path, cancellationToken),
            _ => throw new UnreachableException()
        };

        return (await existingTag, existingTaggableItem);
    }
}
