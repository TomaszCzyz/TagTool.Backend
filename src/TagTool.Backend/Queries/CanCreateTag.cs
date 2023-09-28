using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;

namespace TagTool.Backend.Queries;

public class CanCreateTagQuery : IQuery<OneOf<ErrorResponse, None>>
{
    public required string NewTagName { get; set; }
}

[UsedImplicitly]
public class CanCreateTag : IQueryHandler<CanCreateTagQuery, OneOf<ErrorResponse, None>>
{
    // todo: creating new db connection for each request seams expensive...
    // it would be nice to reuse connection for single streaming call in FileActionService
    private readonly TagToolDbContext _dbContext;

    public CanCreateTag(TagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OneOf<ErrorResponse, None>> Handle(CanCreateTagQuery request, CancellationToken cancellationToken)
    {
        var existingTag = await _dbContext.Tags.FirstOrDefaultAsync(
            tagBase => EF.Functions.Like(tagBase.FormattedName, $"%:{request.NewTagName}"),
            cancellationToken);

        if (existingTag is not null)
        {
            return new ErrorResponse($"Tag with name {request.NewTagName} already exists.");
        }

        return new None();
    }
}
