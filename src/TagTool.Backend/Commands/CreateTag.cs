﻿using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Commands;

public class CreateTagRequest : ICommand<OneOf<TagBase, ErrorResponse>>, IReversible
{
    public required TagBase Tag { get; init; }

    public IReversible GetReverse() => new DeleteTagRequest { Tag = Tag };
}

[UsedImplicitly]
public class CreateTag : ICommandHandler<CreateTagRequest, OneOf<TagBase, ErrorResponse>>
{
    private readonly ILogger<CreateTag> _logger;
    private readonly ITagToolDbContext _dbContext;

    public CreateTag(ILogger<CreateTag> logger, ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OneOf<TagBase, ErrorResponse>> Handle(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var first = await _dbContext.Tags.FirstOrDefaultAsync(tag => tag.FormattedName == request.Tag.FormattedName, cancellationToken);

        if (first is not null)
        {
            return new ErrorResponse($"Tag {request.Tag} already exists.");
        }

        _logger.LogInformation("Creating new tag {@Tag}", request.Tag);

        await _dbContext.Tags.AddAsync(request.Tag, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return request.Tag;
    }
}
