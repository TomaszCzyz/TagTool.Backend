using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts.Internal;
using TagTool.BackendNew.DbContexts;

namespace TagTool.BackendNew.Queries;

using Response = OneOf<Yes, Error<string>>;

public class CanCreateTag : IQuery<Response>
{
    public required string NewTagText { get; init; }
}

[UsedImplicitly]
public class CanCreateTagQueryHandler : IQueryHandler<CanCreateTag, Response>
{
    // todo: creating new db connection for each request seams expensive...
    // it would be nice to reuse connection for single streaming call
    private readonly ITagToolDbContext _dbContext;

    public CanCreateTagQueryHandler(ITagToolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response> Handle(CanCreateTag request, CancellationToken cancellationToken)
    {
        var existingTag = await _dbContext.Tags.FirstOrDefaultAsync(
            tagBase => EF.Functions.Like(tagBase.Text, $"%:{request.NewTagText}"),
            cancellationToken);

        if (existingTag is not null)
        {
            return new Error<string>($"Tag with name {request.NewTagText} already exists.");
        }

        return new Yes();
    }
}
