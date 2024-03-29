﻿using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Commands;

public class DeleteTagRequest : ICommand<OneOf<TagBase, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public bool DeleteUsedToo { get; init; }

    public IReversible GetReverse() => new CreateTagRequest { Tag = Tag };
}

[UsedImplicitly]
public class DeleteTag : ICommandHandler<DeleteTagRequest, OneOf<TagBase, ErrorResponse>>
{
    private readonly ILogger<DeleteTag> _logger;
    private readonly ITagToolDbContext _dbContext;

    public DeleteTag(ILogger<DeleteTag> logger, ITagToolDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<OneOf<TagBase, ErrorResponse>> Handle(DeleteTagRequest request, CancellationToken cancellationToken)
    {
        var existingTag = await _dbContext.Tags
            .Include(tag => tag.TaggedItems)
            .FirstOrDefaultAsync(tag => tag.FormattedName == request.Tag.FormattedName, cancellationToken);

        if (existingTag is null)
        {
            return new ErrorResponse($"Tag {request.Tag} does not exists.");
        }

        if (!request.DeleteUsedToo && existingTag.TaggedItems.Count != 0)
        {
            var message = $"Tag {request.Tag} is in use and it was not deleted. " +
                          $"If you want to delete this tag use {nameof(request.DeleteUsedToo)} flag.";

            return new ErrorResponse(message);
        }

        _logger.LogInformation("Removing tag {@TagName} and all its occurrences in TaggedItems table", existingTag);
        _dbContext.Tags.Remove(existingTag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return request.Tag;
    }
}
